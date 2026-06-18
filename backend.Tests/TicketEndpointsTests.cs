using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TicketFlow.Api.Models;

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
    public async Task TicketCrudFlow_PersistsAndRemovesTicket()
    {
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

    private sealed record HealthResponse(string Status);
}
