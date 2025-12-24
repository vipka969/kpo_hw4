using Microsoft.EntityFrameworkCore;
using PaymentsService.Models.Entities;

namespace PaymentsService.Infrastructure.Data;

public class PaymentsDbContext : DbContext
{
    public DbSet<Account> Accounts { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MessageId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedDate);
            entity.Property(e => e.Payload);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(x => x.Error).IsRequired(false);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EventId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedDate);
            entity.Property(e => e.Payload);
            entity.Property(e => e.Status).HasConversion<string>();
        });
    }
}