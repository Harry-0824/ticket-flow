using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using TicketFlow.Api.Data;
using TicketFlow.Api.Models;

const string CorsPolicyName = "ConfiguredFrontendOrigins";

var builder = WebApplication.CreateBuilder(args);

// 啟動時先集中讀取環境差異，讓本機 SQLite、正式環境 PostgreSQL 與 JWT 設定保持同一套入口。
var databaseProvider = GetDatabaseProvider(builder.Configuration, builder.Environment);
var jwtSettings = GetJwtSettings(builder.Configuration, builder.Environment);
var corsAllowedOrigins = GetCorsAllowedOrigins(builder.Configuration);

builder.Services.AddDbContext<TicketFlowDbContext>(options =>
{
    if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        // Render 正式環境連 Supabase PostgreSQL。註冊 / CRUD 都是非冪等寫入，因此不在 EF Core 層自動重試，
        // 避免連線池回報暫時性錯誤時重送 INSERT，造成資料已寫入但 API 回 500 的情境。
        options.UseNpgsql(GetRequiredConnectionString(builder.Configuration, "TicketFlowPostgres"));
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
            IssuerSigningKey = CreateSigningKey(jwtSettings.Secret),
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

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
        // 開發環境自動套 migration，方便用全新 SQLite 檔快速啟動；正式環境交由部署流程控管。
        db.Database.Migrate();
    }

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

// Auth API 由後端自建；Supabase 在本專案只作 PostgreSQL，不使用 Supabase Auth。
var auth = app.MapGroup("/api/auth").WithTags("Auth");

auth.MapPost("/register", async (
    RegisterRequest request,
    TicketFlowDbContext db,
    IPasswordHasher<ApplicationUser> passwordHasher,
    JwtSettings settings) =>
{
    var validationError = ValidateRegister(request);
    if (validationError is not null)
    {
        return validationError;
    }

    var email = request.Email.Trim();
    var normalizedEmail = NormalizeEmail(email);
    // 以 NormalizedEmail 做唯一檢查，避免大小寫不同造成重複帳號。
    var emailExists = await db.ApplicationUsers.AnyAsync(user => user.NormalizedEmail == normalizedEmail);

    if (emailExists)
    {
        return DuplicateEmail();
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
        return DuplicateEmail();
    }

    return Results.Created("/api/auth/login", CreateAuthResponse(user, settings));
})
    .WithName("Register");

auth.MapPost("/login", async (
    LoginRequest request,
    TicketFlowDbContext db,
    IPasswordHasher<ApplicationUser> passwordHasher,
    JwtSettings settings) =>
{
    var email = request.Email.Trim();
    var normalizedEmail = NormalizeEmail(email);
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

    return Results.Ok(CreateAuthResponse(user, settings));
})
    .WithName("Login");

var tickets = app.MapGroup("/api/tickets").WithTags("Tickets");
// 工單 CRUD 是作品的核心資料，必須登入後才能存取，避免未授權使用者直接操作 API。
tickets.RequireAuthorization();

tickets.MapGet("", async (
    TicketStatus? status,
    TicketPriority? priority,
    string? keyword,
    TicketFlowDbContext db) =>
{
    var query = db.Tickets.AsNoTracking();

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

    return await query.ToListAsync();
})
    .WithName("ListTickets");

tickets.MapGet("/{id:int}", async (int id, TicketFlowDbContext db) =>
{
    var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

    return ticket is null ? Results.NotFound() : Results.Ok(ticket);
})
    .WithName("GetTicket");

tickets.MapPost("", async (Ticket ticket, TicketFlowDbContext db) =>
{
    var validationError = ValidateTicket(ticket);
    if (validationError is not null)
    {
        return validationError;
    }

    var now = DateTime.UtcNow;
    ticket.Id = 0;
    ticket.Title = ticket.Title.Trim();
    ticket.Description = ticket.Description.Trim();
    ticket.Assignee = ticket.Assignee.Trim();
    ticket.CreatedAt = now;
    ticket.UpdatedAt = now;

    db.Tickets.Add(ticket);
    await db.SaveChangesAsync();

    return Results.Created($"/api/tickets/{ticket.Id}", ticket);
})
    .WithName("CreateTicket");

tickets.MapPut("/{id:int}", async (int id, Ticket ticket, TicketFlowDbContext db) =>
{
    var validationError = ValidateTicket(ticket);
    if (validationError is not null)
    {
        return validationError;
    }

    var existingTicket = await db.Tickets.FindAsync(id);

    if (existingTicket is null)
    {
        return Results.NotFound();
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

tickets.MapDelete("/{id:int}", async (int id, TicketFlowDbContext db) =>
{
    var ticket = await db.Tickets.FindAsync(id);

    if (ticket is null)
    {
        return Results.NotFound();
    }

    db.Tickets.Remove(ticket);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
    .WithName("DeleteTicket");

app.Run();

static IResult? ValidateRegister(RegisterRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email.Trim()))
    {
        errors["email"] = ["請輸入有效的 Email。"];
    }
    else if (request.Email.Trim().Length > 320)
    {
        errors["email"] = ["Email 最多 320 個字元。"];
    }

    if (string.IsNullOrWhiteSpace(request.DisplayName))
    {
        errors["displayName"] = ["顯示名稱為必填。"];
    }
    else if (request.DisplayName.Trim().Length > 120)
    {
        errors["displayName"] = ["顯示名稱最多 120 個字元。"];
    }

    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
    {
        errors["password"] = ["密碼至少需要 8 個字元。"];
    }

    return errors.Count == 0
        ? null
        : Results.BadRequest(new ValidationErrorResponse("請修正註冊欄位後再送出。", errors));
}

static IResult? ValidateTicket(Ticket ticket)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(ticket.Title))
    {
        errors["title"] = ["標題為必填。"];
    }
    else if (ticket.Title.Trim().Length > 200)
    {
        errors["title"] = ["標題最多 200 個字元。"];
    }

    if (string.IsNullOrWhiteSpace(ticket.Description))
    {
        errors["description"] = ["描述為必填。"];
    }
    else if (ticket.Description.Trim().Length > 2000)
    {
        errors["description"] = ["描述最多 2000 個字元。"];
    }

    if (ticket.Assignee.Trim().Length > 120)
    {
        errors["assignee"] = ["指派對象最多 120 個字元。"];
    }

    return errors.Count == 0
        ? null
        : Results.BadRequest(new ValidationErrorResponse("請修正工單欄位後再送出。", errors));
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

static JwtSettings GetJwtSettings(IConfiguration configuration, IHostEnvironment environment)
{
    var issuer = configuration["Jwt:Issuer"] ?? "TicketFlow";
    var audience = configuration["Jwt:Audience"] ?? "TicketFlowClient";
    var secret = configuration["Jwt:Secret"];
    var expiresMinutes = configuration.GetValue("Jwt:ExpiresMinutes", 60);

    if (string.IsNullOrWhiteSpace(secret))
    {
        if (environment.IsProduction())
        {
            throw new InvalidOperationException("JWT secret is required in production.");
        }

        // 只有非正式環境允許 fallback secret；正式部署一定要由 Render env var 提供。
        secret = "ticket-flow-local-development-signing-key";
    }

    if (Encoding.UTF8.GetByteCount(secret) < 32)
    {
        throw new InvalidOperationException("JWT secret must be at least 32 bytes.");
    }

    if (expiresMinutes <= 0)
    {
        throw new InvalidOperationException("JWT expiration minutes must be greater than 0.");
    }

    return new JwtSettings(issuer, audience, secret, expiresMinutes);
}

static AuthResponse CreateAuthResponse(ApplicationUser user, JwtSettings settings)
{
    var expiresAt = DateTime.UtcNow.AddMinutes(settings.ExpiresMinutes);
    // token claims 只放前端需要辨識登入者的最小資料，權限模型之後可再擴充。
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.DisplayName)
    };
    var token = new JwtSecurityToken(
        issuer: settings.Issuer,
        audience: settings.Audience,
        claims: claims,
        expires: expiresAt,
        signingCredentials: new SigningCredentials(
            CreateSigningKey(settings.Secret),
            SecurityAlgorithms.HmacSha256));

    return new AuthResponse(
        new JwtSecurityTokenHandler().WriteToken(token),
        expiresAt,
        new AuthUserResponse(user.Id, user.Email, user.DisplayName));
}

static SymmetricSecurityKey CreateSigningKey(string secret) =>
    new(Encoding.UTF8.GetBytes(secret));

static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

static bool IsValidEmail(string email)
{
    try
    {
        return string.Equals(new MailAddress(email).Address, email, StringComparison.OrdinalIgnoreCase);
    }
    catch (FormatException)
    {
        return false;
    }
}

static IResult InvalidLogin() =>
    Results.Json(
        new ErrorResponse("Email 或密碼不正確。"),
        statusCode: StatusCodes.Status401Unauthorized);

static IResult DuplicateEmail() =>
    Results.BadRequest(new ValidationErrorResponse(
        "請修正註冊欄位後再送出。",
        new Dictionary<string, string[]>
        {
            ["email"] = ["Email 已被註冊。"]
        }));

static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
    exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } ||
    exception.InnerException is SqliteException { SqliteErrorCode: 19 };

sealed record ValidationErrorResponse(string Message, Dictionary<string, string[]> Errors);

sealed record RegisterRequest(string Email, string DisplayName, string Password);

sealed record LoginRequest(string Email, string Password);

sealed record AuthUserResponse(int Id, string Email, string DisplayName);

sealed record AuthResponse(string Token, DateTime ExpiresAt, AuthUserResponse User);

sealed record ErrorResponse(string Message);

sealed record JwtSettings(string Issuer, string Audience, string Secret, int ExpiresMinutes);

public partial class Program
{
}
