using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.Api.Models;
using TicketFlow.Api.Data;

namespace TicketFlow.Api.Tests;

public class TicketEndpointsTests(TicketFlowApiFactory factory)
    : IClassFixture<TicketFlowApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Health_ReturnsHealthyStatus()
    {
        var response = await client.GetFromJsonAsync<HealthResponse>("/health");

        Assert.Equal("Healthy", response?.Status);
    }

    [Fact]
    public async Task RegisterLoginFlow_ReturnsJwtAndStoresPasswordHash()
    {
        var email = CreateUniqueEmail();
        const string password = "Password123!";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Alex Chen",
            password
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(registered?.Token);
        Assert.Equal(email, registered.User.Email);

        var duplicateResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = email.ToUpperInvariant(),
            displayName = "Alex Chen",
            password
        });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loggedIn = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(loggedIn?.Token);
        Assert.Equal(email, loggedIn.User.Email);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
        var user = db.ApplicationUsers.Single(item => item.Email == email);
        Assert.NotEqual(password, user.PasswordHash);
        Assert.NotEmpty(user.PasswordHash);
    }

    [Fact]
    public async Task Register_WhenSaveThrowsAfterPersistingUser_ReturnsCreatedAuthResponse()
    {
        var email = CreateUniqueEmail();
        var normalizedEmail = email.ToUpperInvariant();
        const string password = "Password123!";
        using var postCommitFactory = new TicketFlowApiFactory(
            new ThrowAfterPersistingUserInterceptor(normalizedEmail));
        using var postCommitClient = postCommitFactory.CreateClient();

        var registerResponse = await postCommitClient.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Post Commit User",
            password
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(registered?.Token);
        Assert.Equal(email, registered.User.Email);

        var loginResponse = await postCommitClient.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task TicketApi_RejectsAnonymousRequests()
    {
        var response = await client.GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TicketCrudFlow_PersistsAndRemovesTicket()
    {
        await RegisterAndAuthorizeAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new
        {
            title = "Login issue",
            description = "User cannot sign in from the support portal.",
            status = "Open",
            priority = "High",
            assignee = "Alex"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdTicket = await createResponse.Content.ReadFromJsonAsync<Ticket>(JsonOptions);
        Assert.NotNull(createdTicket);
        Assert.True(createdTicket.Id > 0);
        Assert.Equal("Login issue", createdTicket.Title);

        var list = await client.GetFromJsonAsync<List<Ticket>>(
            "/api/tickets?status=Open&priority=High&keyword=sign",
            JsonOptions);
        Assert.Single(list!);
        Assert.Equal(createdTicket.Id, list![0].Id);

        var detail = await client.GetFromJsonAsync<Ticket>(
            $"/api/tickets/{createdTicket.Id}",
            JsonOptions);
        Assert.Equal("Alex", detail?.Assignee);

        var updateResponse = await client.PutAsJsonAsync($"/api/tickets/{createdTicket.Id}", new
        {
            title = "Login issue resolved",
            description = "Support confirmed the user can sign in.",
            status = "Done",
            priority = "Medium",
            assignee = "Jamie"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedTicket = await updateResponse.Content.ReadFromJsonAsync<Ticket>(JsonOptions);
        Assert.Equal(TicketStatus.Done, updatedTicket?.Status);
        Assert.Equal("Jamie", updatedTicket?.Assignee);

        var deleteResponse = await client.DeleteAsync($"/api/tickets/{createdTicket.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var missingResponse = await client.GetAsync($"/api/tickets/{createdTicket.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task CreateTicket_RejectsInvalidInput()
    {
        await RegisterAndAuthorizeAsync();

        var response = await client.PostAsJsonAsync("/api/tickets", new
        {
            title = " ",
            description = "",
            status = "Open",
            priority = "Medium",
            assignee = new string('a', 121)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var validation = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.Equal("請修正工單欄位後再送出。", validation?.Message);
        Assert.Contains("title", validation!.Errors.Keys);
        Assert.Contains("description", validation.Errors.Keys);
        Assert.Contains("assignee", validation.Errors.Keys);
    }

    [Fact]
    public async Task UpdateTicket_RejectsInvalidInput()
    {
        await RegisterAndAuthorizeAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new
        {
            title = "Billing issue",
            description = "Invoice total does not match the contract.",
            status = "Open",
            priority = "Medium",
            assignee = ""
        });
        var createdTicket = await createResponse.Content.ReadFromJsonAsync<Ticket>(JsonOptions);

        var response = await client.PutAsJsonAsync($"/api/tickets/{createdTicket!.Id}", new
        {
            title = new string('x', 201),
            description = " ",
            status = "Open",
            priority = "Medium",
            assignee = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var validation = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.Contains("title", validation!.Errors.Keys);
        Assert.Contains("description", validation.Errors.Keys);
    }

    private async Task<AuthResponse> RegisterAndAuthorizeAsync()
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = CreateUniqueEmail(),
            displayName = "Test User",
            password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth?.Token);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        return auth;
    }

    private static string CreateUniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private sealed class ThrowAfterPersistingUserInterceptor(string normalizedEmail) : SaveChangesInterceptor
    {
        private bool hasThrown;

        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            var shouldThrow = !hasThrown &&
                eventData.Context?.ChangeTracker.Entries<ApplicationUser>().Any(entry =>
                    entry.Entity.NormalizedEmail == normalizedEmail &&
                    entry.State == EntityState.Unchanged) == true;

            if (shouldThrow)
            {
                hasThrown = true;
                throw new InvalidOperationException("Simulated post-commit register failure.");
            }

            return new ValueTask<int>(result);
        }
    }

    private sealed record HealthResponse(string Status);

    private sealed record ValidationErrorResponse(
        string Message,
        Dictionary<string, string[]> Errors);

    private sealed record AuthUserResponse(int Id, string Email, string DisplayName);

    private sealed record AuthResponse(string Token, DateTime ExpiresAt, AuthUserResponse User);
}
