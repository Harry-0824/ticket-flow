using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TicketFlow.Api.Data;
using TicketFlow.Api.Models;
using TicketFlow.Api.Services;

public static class AuthEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
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
    }

    private static IResult InvalidLogin() =>
        Results.Json(
            new ErrorResponse("Email 或密碼不正確。"),
            statusCode: StatusCodes.Status401Unauthorized);

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } ||
        exception.InnerException is SqliteException { SqliteErrorCode: 19 };
}

public sealed record RegisterRequest(string Email, string DisplayName, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record ErrorResponse(string Message);
