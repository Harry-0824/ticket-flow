using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TicketFlow.Api.Data;
using TicketFlow.Api.Models;

public static class TicketEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var tickets = app.MapGroup("/api/tickets").WithTags("Tickets");
        tickets.RequireAuthorization();

        tickets.MapGet("", async (
            TicketStatus? status,
            TicketPriority? priority,
            string? keyword,
            TicketFlowDbContext db,
            HttpContext httpContext) =>
        {
            var currentUserId = GetCurrentUserId(httpContext);
            if (currentUserId is null)
            {
                return Results.Unauthorized();
            }

            var query = db.Tickets
                .AsNoTracking()
                .Where(ticket => ticket.UserId == currentUserId.Value);

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

            return Results.Ok(await query.ToListAsync());
        })
            .WithName("ListTickets");

        tickets.MapGet("/{id:int}", async (int id, TicketFlowDbContext db, HttpContext httpContext) =>
        {
            var currentUserId = GetCurrentUserId(httpContext);
            if (currentUserId is null)
            {
                return Results.Unauthorized();
            }

            var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

            if (ticket is null)
            {
                return Results.NotFound();
            }

            return ticket.UserId == currentUserId.Value ? Results.Ok(ticket) : Results.Forbid();
        })
            .WithName("GetTicket");

        tickets.MapPost("", async (Ticket ticket, TicketFlowDbContext db, HttpContext httpContext) =>
        {
            var currentUserId = GetCurrentUserId(httpContext);
            if (currentUserId is null)
            {
                return Results.Unauthorized();
            }

            var validationError = TicketValidator.Validate(ticket);
            if (validationError is not null)
            {
                return validationError;
            }

            var now = DateTime.UtcNow;
            ticket.Id = 0;
            ticket.Title = ticket.Title.Trim();
            ticket.Description = ticket.Description.Trim();
            ticket.Assignee = ticket.Assignee.Trim();
            ticket.UserId = currentUserId.Value;
            ticket.CreatedAt = now;
            ticket.UpdatedAt = now;

            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();

            return Results.Created($"/api/tickets/{ticket.Id}", ticket);
        })
            .WithName("CreateTicket");

        tickets.MapPut("/{id:int}", async (int id, Ticket ticket, TicketFlowDbContext db, HttpContext httpContext) =>
        {
            var currentUserId = GetCurrentUserId(httpContext);
            if (currentUserId is null)
            {
                return Results.Unauthorized();
            }

            var validationError = TicketValidator.Validate(ticket);
            if (validationError is not null)
            {
                return validationError;
            }

            var existingTicket = await db.Tickets.FindAsync(id);

            if (existingTicket is null)
            {
                return Results.NotFound();
            }

            if (existingTicket.UserId != currentUserId.Value)
            {
                return Results.Forbid();
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

        tickets.MapDelete("/{id:int}", async (int id, TicketFlowDbContext db, HttpContext httpContext) =>
        {
            var currentUserId = GetCurrentUserId(httpContext);
            if (currentUserId is null)
            {
                return Results.Unauthorized();
            }

            var ticket = await db.Tickets.FindAsync(id);

            if (ticket is null)
            {
                return Results.NotFound();
            }

            if (ticket.UserId != currentUserId.Value)
            {
                return Results.Forbid();
            }

            db.Tickets.Remove(ticket);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
            .WithName("DeleteTicket");
    }

    private static int? GetCurrentUserId(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
    }
}
