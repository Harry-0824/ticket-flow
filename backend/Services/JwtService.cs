using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TicketFlow.Api.Models;

namespace TicketFlow.Api.Services;

public sealed class JwtService(JwtSettings settings)
{
    public static JwtSettings GetJwtSettings(
        IConfiguration configuration,
        IHostEnvironment environment,
        int defaultExpiresMinutes,
        int maxExpiresMinutes)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "TicketFlow";
        var audience = configuration["Jwt:Audience"] ?? "TicketFlowClient";
        var secret = configuration["Jwt:Secret"];
        var expiresMinutes = configuration.GetValue("Jwt:ExpiresMinutes", defaultExpiresMinutes);

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

        if (expiresMinutes > maxExpiresMinutes)
        {
            // 部署平台的 env var 若誤填過大的數字，DateTime.AddMinutes 會在登入/註冊時才爆 500。
            // 對作品 demo 來說，回到 60 分鐘比讓正式 auth flow 整段不可用更安全。
            expiresMinutes = defaultExpiresMinutes;
        }

        return new JwtSettings(issuer, audience, secret, expiresMinutes);
    }

    public static SymmetricSecurityKey CreateSigningKey(string secret) =>
        new(Encoding.UTF8.GetBytes(secret));

    public IResult CreateAuthResult(
        ApplicationUser user,
        ILogger logger,
        string operation,
        Func<AuthResponse, IResult> createResult)
    {
        try
        {
            return createResult(CreateAuthResponse(user));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Auth response creation failed during {Operation} for user id {UserId}. Check JWT issuer, audience, secret length, and expiration settings.",
                operation,
                user.Id);

            return Results.Problem(
                title: "Auth response creation failed.",
                detail: "Authentication succeeded, but the server could not create the auth response. Check server logs for JWT configuration details.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    public AuthResponse CreateAuthResponse(ApplicationUser user)
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
}

public sealed record JwtSettings(string Issuer, string Audience, string Secret, int ExpiresMinutes);

public sealed record AuthUserResponse(int Id, string Email, string DisplayName);

public sealed record AuthResponse(string Token, DateTime ExpiresAt, AuthUserResponse User);
