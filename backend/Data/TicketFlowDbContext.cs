using Microsoft.EntityFrameworkCore;
using TicketFlow.Api.Models;

namespace TicketFlow.Api.Data;

public class TicketFlowDbContext(DbContextOptions<TicketFlowDbContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(ticket =>
        {
            ticket.ToTable("Tickets");
            ticket.HasKey(item => item.Id);
            ticket.Property(item => item.Title).HasMaxLength(200).IsRequired();
            ticket.Property(item => item.Description).HasMaxLength(2000).IsRequired();
            ticket.Property(item => item.Status).IsRequired();
            ticket.Property(item => item.Priority).IsRequired();
            ticket.Property(item => item.RequesterName).HasMaxLength(120).IsRequired();
        });
    }
}
