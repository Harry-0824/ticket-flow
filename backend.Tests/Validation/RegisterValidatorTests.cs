namespace TicketFlow.Api.Tests.Validation;

public class RegisterValidatorTests
{
    [Fact]
    public void Validate_WithValidRequest_ReturnsNull()
    {
        var request = new RegisterRequest("user@example.com", "Test User", "Password123!");

        var result = RegisterValidator.Validate(request);

        Assert.Null(result);
    }

    [Fact]
    public void Validate_WithInvalidFields_ReturnsBadRequestResult()
    {
        var request = new RegisterRequest("invalid", "", "short");

        var result = RegisterValidator.Validate(request);

        Assert.NotNull(result);
    }

    [Fact]
    public void NormalizeEmail_TrimsAndUpperCasesEmail()
    {
        var normalized = RegisterValidator.NormalizeEmail("  User@Example.com  ");

        Assert.Equal("USER@EXAMPLE.COM", normalized);
    }

    [Fact]
    public void DuplicateEmail_ReturnsBadRequestResult()
    {
        var result = RegisterValidator.DuplicateEmail();

        Assert.NotNull(result);
    }
}
