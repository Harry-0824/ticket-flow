using Microsoft.EntityFrameworkCore;
using TicketFlow.Api.Data;
using TicketFlow.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TicketFlowDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TicketFlow")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
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
    var now = DateTime.UtcNow;
    ticket.Id = 0;
    ticket.CreatedAt = now;
    ticket.UpdatedAt = now;

    db.Tickets.Add(ticket);
    await db.SaveChangesAsync();

    return Results.Created($"/api/tickets/{ticket.Id}", ticket);
})
    .WithName("CreateTicket");

tickets.MapPut("/{id:int}", async (int id, Ticket ticket, TicketFlowDbContext db) =>
{
    var existingTicket = await db.Tickets.FindAsync(id);

    if (existingTicket is null)
    {
        return Results.NotFound();
    }

    existingTicket.Title = ticket.Title;
    existingTicket.Description = ticket.Description;
    existingTicket.Status = ticket.Status;
    existingTicket.Priority = ticket.Priority;
    existingTicket.Assignee = ticket.Assignee;
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
