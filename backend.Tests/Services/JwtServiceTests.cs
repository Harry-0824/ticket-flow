using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TicketFlow.Api.Models;
using TicketFlow.Api.Services;

namespace TicketFlow.Api.Tests.Services;

public class JwtServiceTests
{
    [Fact]
    public void GetJwtSettings_WhenExpiryExceedsMaximum_UsesDefaultExpiry()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "ticket-flow-test-signing-key-at-least-32-bytes",
                ["Jwt:ExpiresMinutes"] = "2147483647"
            })
            .Build();

        var settings = JwtService.GetJwtSettings(
            configuration,
            new TestHostEnvironment("Testing"),
            defaultExpiresMinutes: 60,
            maxExpiresMinutes: 1440);

        Assert.Equal(60, settings.ExpiresMinutes);
    }

    [Fact]
    public void CreateAuthResponse_ReturnsTokenAndUserPayload()
    {
        var service = new JwtService(new JwtSettings(
            "TicketFlow",
            "TicketFlowClient",
            "ticket-flow-test-signing-key-at-least-32-bytes",
            60));
        var user = new ApplicationUser
        {
            Id = 42,
            Email = "user@example.com",
            DisplayName = "Test User"
        };

        var response = service.CreateAuthResponse(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);

        Assert.Equal(42, response.User.Id);
        Assert.Equal("user@example.com", response.User.Email);
        Assert.Equal("Test User", response.User.DisplayName);
        Assert.Equal("TicketFlow", token.Issuer);
        Assert.Equal("TicketFlowClient", token.Audiences.Single());
        Assert.Equal("42", token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("42", token.Claims.Single(claim => claim.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("user@example.com", token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Test User", token.Claims.Single(claim => claim.Type == ClaimTypes.Name).Value);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "TicketFlow.Api.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
