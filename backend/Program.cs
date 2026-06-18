using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using TicketFlow.Api.Data;
using TicketFlow.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TicketFlowDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TicketFlow")));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();
        db.Database.Migrate();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
    .WithName("GetHealth");

var tickets = app.MapGroup("/api/tickets").WithTags("Tickets");

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

sealed record ValidationErrorResponse(string Message, Dictionary<string, string[]> Errors);

public partial class Program
{
}
