using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;
using TicketFlow.Api.Data;
using TicketFlow.Api.Models;
using TicketFlow.Api.Services;

const string CorsPolicyName = "ConfiguredFrontendOrigins";
const int DefaultJwtExpiresMinutes = 60;
const int MaxJwtExpiresMinutes = 1440;
const int PostgresCommandTimeoutSeconds = 60;

var builder = WebApplication.CreateBuilder(args);

// 啟動時先集中讀取環境差異，讓本機 SQLite、正式環境 PostgreSQL 與 JWT 設定保持同一套入口。
var databaseProvider = GetDatabaseProvider(builder.Configuration, builder.Environment);
var jwtSettings = JwtService.GetJwtSettings(
    builder.Configuration,
    builder.Environment,
    DefaultJwtExpiresMinutes,
    MaxJwtExpiresMinutes);
var corsAllowedOrigins = GetCorsAllowedOrigins(builder.Configuration);

builder.Services.AddDbContext<TicketFlowDbContext>(options =>
{
    if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        // Render 正式環境連 Supabase PostgreSQL。註冊 / CRUD 都是非冪等寫入，因此不在 EF Core 層自動重試，
        // 避免連線池回報暫時性錯誤時重送 INSERT，造成資料已寫入但 API 回 500 的情境。
        // Render Free + Supabase pooler 偶爾會在讀取回應時超過 Npgsql 預設 30 秒。
        // 先拉長 timeout，不啟用全域 retry，避免非冪等 INSERT 在連線逾時後被重送。
        options.UseNpgsql(
            GetRequiredConnectionString(builder.Configuration, "TicketFlowPostgres"),
            postgresOptions => postgresOptions.CommandTimeout(PostgresCommandTimeoutSeconds));
        return;
    }

    if (databaseProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
    {
        // 本機預設保留 SQLite，讓面試 demo 開發不需要先安裝 PostgreSQL。
        options.UseSqlite(GetRequiredConnectionString(builder.Configuration, "TicketFlow"));
        return;
    }

    throw new InvalidOperationException(
        $"Unsupported database provider '{databaseProvider}'. Use 'SQLite' or 'PostgreSQL'.");
});
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<JwtService>();
// 使用 ASP.NET Core Identity 內建 hasher，只採用密碼雜湊能力，不引入完整 Identity 使用者系統。
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
if (corsAllowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(CorsPolicyName, policy =>
            policy.WithOrigins(corsAllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
}
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // API 只接受後端簽出的 JWT；issuer/audience/secret 必須與登入回傳 token 的設定一致。
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = JwtService.CreateSigningKey(jwtSettings.Secret),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

var app = builder.Build();

// Migration 在 Development 與 Production 都自動套用，避免部署後資料庫 schema 落後於 code。
// 小作品可接受；正式產品建議改由 CI/CD migration pipeline 控管。
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (corsAllowedOrigins.Length > 0)
{
    app.UseCors(CorsPolicyName);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
    .WithName("GetHealth");

app.MapGet("/build-info", (IHostEnvironment environment, IConfiguration configuration) =>
    Results.Ok(new
    {
        commit = GetFirstConfiguredValue(
            configuration,
            "RENDER_GIT_COMMIT",
            "GIT_COMMIT_SHA",
            "GIT_COMMIT",
            "COMMIT_SHA"),
        environment = string.IsNullOrWhiteSpace(environment.EnvironmentName)
            ? "unknown"
            : environment.EnvironmentName,
        buildTime = GetFirstConfiguredValue(
            configuration,
            "BUILD_TIME",
            "BUILD_TIME_UTC",
            "RENDER_BUILD_TIME")
    }))
    .WithName("GetBuildInfo");

// Auth API 由後端自建；Supabase 在本專案只作 PostgreSQL，不使用 Supabase Auth。
var auth = app.MapGroup("/api/auth").WithTags("Auth");

auth.MapPost("/register", async (
    RegisterRequest request,
    TicketFlowDbContext db,
    IPasswordHasher<ApplicationUser> passwordHasher,
    ILogger<Program> logger,
    JwtService jwtService) =>
{
    var validationError = RegisterValidator.Validate(request);
    if (validationError is not null)
    {
        return validationError;
    }

    var email = request.Email.Trim();
    var normalizedEmail = RegisterValidator.NormalizeEmail(email);
    // 以 NormalizedEmail 做唯一檢查，避免大小寫不同造成重複帳號。
    var emailExists = await db.ApplicationUsers.AnyAsync(user => user.NormalizedEmail == normalizedEmail);

    if (emailExists)
    {
        return RegisterValidator.DuplicateEmail();
    }

    var now = DateTime.UtcNow;
    var user = new ApplicationUser
    {
        Email = email,
        NormalizedEmail = normalizedEmail,
        DisplayName = request.DisplayName.Trim(),
        CreatedAt = now,
        UpdatedAt = now
    };
    // PasswordHash 是唯一會被保存的密碼資料，後端不儲存明文 password。
    user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

    db.ApplicationUsers.Add(user);
    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
    {
        // AnyAsync 是友善的預先檢查，但正式環境仍可能遇到同 email 併發註冊。
        // 唯一索引才是最後防線，這裡把資料庫例外轉成一致的 validation response。
        return RegisterValidator.DuplicateEmail();
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
        // Supabase pooler / Npgsql 可能在 INSERT 已 commit 後，因連線回收或 response 階段錯誤丟例外。
        // 重新查詢可把「資料已成功寫入」的情境轉回正常註冊回應；若查不到則保留原始 500。
        logger.LogWarning(exception, "Register save failed; checking whether the user was persisted.");
        db.ChangeTracker.Clear();

        var persistedUser = await db.ApplicationUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.NormalizedEmail == normalizedEmail);

        if (persistedUser is null)
        {
            throw;
        }

        var passwordResult = passwordHasher.VerifyHashedPassword(
            persistedUser,
            persistedUser.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return RegisterValidator.DuplicateEmail();
        }

        return jwtService.CreateAuthResult(
            persistedUser,
            logger,
            "register-post-commit-reconcile",
            response => Results.Created("/api/auth/login", response));
    }

    return jwtService.CreateAuthResult(
        user,
        logger,
        "register",
        response => Results.Created("/api/auth/login", response));
})
    .WithName("Register");

auth.MapPost("/login", async (
    LoginRequest request,
    TicketFlowDbContext db,
    IPasswordHasher<ApplicationUser> passwordHasher,
    ILogger<Program> logger,
    JwtService jwtService) =>
{
    var email = request.Email.Trim();
    var normalizedEmail = RegisterValidator.NormalizeEmail(email);
    var user = await db.ApplicationUsers.FirstOrDefaultAsync(item => item.NormalizedEmail == normalizedEmail);

    if (user is null)
    {
        return InvalidLogin();
    }

    var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
    if (result == PasswordVerificationResult.Failed)
    {
        return InvalidLogin();
    }

    return jwtService.CreateAuthResult(
        user,
        logger,
        "login",
        response => Results.Ok(response));
})
    .WithName("Login");

var tickets = app.MapGroup("/api/tickets").WithTags("Tickets");
// 工單 CRUD 是作品的核心資料，必須登入後才能存取，避免未授權使用者直接操作 API。
tickets.RequireAuthorization();

tickets.MapGet("", async (
    TicketStatus? status,
    TicketPriority? priority,
    string? keyword,
    TicketFlowDbContext db,
    HttpContext httpContext) =>
{
    var currentUserId = GetCurrentUserId(httpContext);
    if (currentUserId is null)
    {
        return Results.Unauthorized();
    }

    var query = db.Tickets
        .AsNoTracking()
        .Where(ticket => ticket.UserId == currentUserId.Value);

    if (status is not null)
    {
        query = query.Where(ticket => ticket.Status == status);
    }

    if (priority is not null)
    {
        query = query.Where(ticket => ticket.Priority == priority);
    }

    if (!string.IsNullOrWhiteSpace(keyword))
    {
        var searchTerm = keyword.Trim();
        query = query.Where(ticket =>
            ticket.Title.Contains(searchTerm) ||
            ticket.Description.Contains(searchTerm));
    }

    return Results.Ok(await query.ToListAsync());
})
    .WithName("ListTickets");

tickets.MapGet("/{id:int}", async (int id, TicketFlowDbContext db, HttpContext httpContext) =>
{
    var currentUserId = GetCurrentUserId(httpContext);
    if (currentUserId is null)
    {
        return Results.Unauthorized();
    }

    var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

    if (ticket is null)
    {
        return Results.NotFound();
    }

    return ticket.UserId == currentUserId.Value ? Results.Ok(ticket) : Results.Forbid();
})
    .WithName("GetTicket");

tickets.MapPost("", async (Ticket ticket, TicketFlowDbContext db, HttpContext httpContext) =>
{
    var currentUserId = GetCurrentUserId(httpContext);
    if (currentUserId is null)
    {
        return Results.Unauthorized();
    }

    var validationError = TicketValidator.Validate(ticket);
    if (validationError is not null)
    {
        return validationError;
    }

    var now = DateTime.UtcNow;
    ticket.Id = 0;
    ticket.Title = ticket.Title.Trim();
    ticket.Description = ticket.Description.Trim();
    ticket.Assignee = ticket.Assignee.Trim();
    ticket.UserId = currentUserId.Value;
    ticket.CreatedAt = now;
    ticket.UpdatedAt = now;

    db.Tickets.Add(ticket);
    await db.SaveChangesAsync();

    return Results.Created($"/api/tickets/{ticket.Id}", ticket);
})
    .WithName("CreateTicket");

tickets.MapPut("/{id:int}", async (int id, Ticket ticket, TicketFlowDbContext db, HttpContext httpContext) =>
{
    var currentUserId = GetCurrentUserId(httpContext);
    if (currentUserId is null)
    {
        return Results.Unauthorized();
    }

    var validationError = TicketValidator.Validate(ticket);
    if (validationError is not null)
    {
        return validationError;
    }

    var existingTicket = await db.Tickets.FindAsync(id);

    if (existingTicket is null)
    {
        return Results.NotFound();
    }

    if (existingTicket.UserId != currentUserId.Value)
    {
        return Results.Forbid();
    }

    existingTicket.Title = ticket.Title.Trim();
    existingTicket.Description = ticket.Description.Trim();
    existingTicket.Status = ticket.Status;
    existingTicket.Priority = ticket.Priority;
    existingTicket.Assignee = ticket.Assignee.Trim();
    existingTicket.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.Ok(existingTicket);
})
    .WithName("UpdateTicket");

tickets.MapDelete("/{id:int}", async (int id, TicketFlowDbContext db, HttpContext httpContext) =>
{
    var currentUserId = GetCurrentUserId(httpContext);
    if (currentUserId is null)
    {
        return Results.Unauthorized();
    }

    var ticket = await db.Tickets.FindAsync(id);

    if (ticket is null)
    {
        return Results.NotFound();
    }

    if (ticket.UserId != currentUserId.Value)
    {
        return Results.Forbid();
    }

    db.Tickets.Remove(ticket);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
    .WithName("DeleteTicket");

app.Run();

static int? GetCurrentUserId(HttpContext httpContext)
{
    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

    return int.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
}

static string GetDatabaseProvider(IConfiguration configuration, IHostEnvironment environment)
{
    var configuredProvider = configuration["Database:Provider"];

    if (!string.IsNullOrWhiteSpace(configuredProvider))
    {
        return configuredProvider;
    }

    // 沒有明確設定時，本機走 SQLite、正式環境走 PostgreSQL，降低部署時接錯資料庫的風險。
    return environment.IsProduction() ? "PostgreSQL" : "SQLite";
}

static string GetRequiredConnectionString(IConfiguration configuration, string name)
{
    return configuration.GetConnectionString(name)
        ?? throw new InvalidOperationException($"Connection string '{name}' is required.");
}

static string[] GetCorsAllowedOrigins(IConfiguration configuration)
{
    var configuredOrigins = configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    if (configuredOrigins.Length == 0)
    {
        configuredOrigins = (configuration["Cors:AllowedOrigins"] ?? string.Empty)
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    return configuredOrigins
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

static string GetFirstConfiguredValue(IConfiguration configuration, params string[] keys)
{
    foreach (var key in keys)
    {
        var value = configuration[key];

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    return "unknown";
}

static IResult InvalidLogin() =>
    Results.Json(
        new ErrorResponse("Email 或密碼不正確。"),
        statusCode: StatusCodes.Status401Unauthorized);

static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
    exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } ||
    exception.InnerException is SqliteException { SqliteErrorCode: 19 };

sealed record ValidationErrorResponse(string Message, Dictionary<string, string[]> Errors);

public sealed record RegisterRequest(string Email, string DisplayName, string Password);

sealed record LoginRequest(string Email, string Password);

sealed record ErrorResponse(string Message);

public partial class Program
{
}
