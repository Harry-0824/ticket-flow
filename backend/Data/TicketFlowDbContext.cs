using Microsoft.EntityFrameworkCore;
using TicketFlow.Api.Models;

namespace TicketFlow.Api.Data;

public class TicketFlowDbContext(DbContextOptions<TicketFlowDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(user =>
        {
            user.ToTable("ApplicationUsers");
            user.HasKey(item => item.Id);
            user.HasIndex(item => item.NormalizedEmail).IsUnique();
            user.Property(item => item.Email).HasMaxLength(320).IsRequired();
            user.Property(item => item.NormalizedEmail).HasMaxLength(320).IsRequired();
            user.Property(item => item.DisplayName).HasMaxLength(120).IsRequired();
            user.Property(item => item.PasswordHash).HasMaxLength(500).IsRequired();
            user.Property(item => item.CreatedAt).IsRequired();
            user.Property(item => item.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<Ticket>(ticket =>
        {
            ticket.ToTable("Tickets");
            ticket.HasKey(item => item.Id);
            ticket.Property(item => item.Title).HasMaxLength(200).IsRequired();
            ticket.Property(item => item.Description).HasMaxLength(2000).IsRequired();
            ticket.Property(item => item.Status).IsRequired();
            ticket.Property(item => item.Priority).IsRequired();
            ticket.Property(item => item.Assignee).HasMaxLength(120).IsRequired();
            ticket.Property(item => item.CreatedAt).IsRequired();
            ticket.Property(item => item.UpdatedAt).IsRequired();
        });
    }
}
