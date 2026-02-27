using Microsoft.EntityFrameworkCore;
using OrderApi.Domain.Entities;

namespace OrderApi.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    
    public DbSet<BalanceReservation> BalanceReservations => Set<BalanceReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdempotencyRecord>()
            .HasIndex(x => x.Key)
            .IsUnique();
        
        modelBuilder.Entity<OutboxEvent>()
            .HasIndex(x => x.Status);
        
        modelBuilder.Entity<BalanceReservation>()
            .HasIndex(x => x.OrderId)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
