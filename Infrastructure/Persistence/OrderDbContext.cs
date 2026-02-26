using Microsoft.EntityFrameworkCore;
using OrderApi.Domain.Entities;

namespace OrderApi.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdempotencyRecord>()
            .HasIndex(x => x.Key)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
