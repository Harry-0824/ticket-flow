using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using TicketFlow.Api.Data;
using TicketFlow.Api.Models;
using TicketFlow.Api.Services;
const string CorsPolicyName = "ConfiguredFrontendOrigins";
const int DefaultJwtExpiresMinutes = 60;
const int MaxJwtExpiresMinutes = 1440;
const int PostgresCommandTimeoutSeconds = 60;
var builder = WebApplication.CreateBuilder(args);
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
        options.UseNpgsql(
            GetRequiredConnectionString(builder.Configuration, "TicketFlowPostgres"),
            postgresOptions => postgresOptions.CommandTimeout(PostgresCommandTimeoutSeconds));
        return;
    }
    if (databaseProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(GetRequiredConnectionString(builder.Configuration, "TicketFlow"));
        return;
    }
    throw new InvalidOperationException(
        $"Unsupported database provider '{databaseProvider}'. Use 'SQLite' or 'PostgreSQL'.");
});
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<JwtService>();
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
AuthEndpoints.MapEndpoints(app);
TicketEndpoints.MapEndpoints(app);
app.Run();
static string GetDatabaseProvider(IConfiguration configuration, IHostEnvironment environment)
{
    var configuredProvider = configuration["Database:Provider"];
    if (!string.IsNullOrWhiteSpace(configuredProvider))
    {
        return configuredProvider;
    }
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
sealed record ValidationErrorResponse(string Message, Dictionary<string, string[]> Errors);
public partial class Program
{
}
