using System.Net.Mail;

public static class RegisterValidator
{
    public static IResult? Validate(RegisterRequest request)
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

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    public static IResult DuplicateEmail() =>
        Results.BadRequest(new ValidationErrorResponse(
            "請修正註冊欄位後再送出。",
            new Dictionary<string, string[]>
            {
                ["email"] = ["Email 已被註冊。"]
            }));

    private static bool IsValidEmail(string email)
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
}
