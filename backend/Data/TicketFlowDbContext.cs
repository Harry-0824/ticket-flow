using Microsoft.EntityFrameworkCore;
using TicketFlow.Api.Models;

namespace TicketFlow.Api.Data;

public class TicketFlowDbContext(DbContextOptions<TicketFlowDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Fluent API 明確定義欄位限制，讓 SQLite 本機與 PostgreSQL 正式環境共用同一份模型規則。
        modelBuilder.Entity<ApplicationUser>(user =>
        {
            user.ToTable("ApplicationUsers");
            user.HasKey(item => item.Id);
            // Email 唯一性用正規化欄位處理，避免大小寫不同繞過註冊檢查。
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
            // 長度限制與 API validation 保持一致，資料庫層也能擋下異常輸入。
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
