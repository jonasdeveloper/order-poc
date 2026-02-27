using Microsoft.EntityFrameworkCore;
using OrderApi.Application.Interfaces;
using OrderApi.Domain.Entities;
using OrderApi.Infrastructure.Persistence;

namespace OrderApi.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly OrderDbContext _context;

    public OutboxRepository(OrderDbContext context) => _context = context;

    public async Task AddAsync(OutboxEvent evt) =>
        await _context.OutboxEvents.AddAsync(evt);

    public async Task<List<OutboxEvent>> GetPendingAsync(int take) =>
        await _context.OutboxEvents
            .Where(x => x.Status == "PENDING" || (x.Status == "FAILED" && x.AttemptCount < 10))
            .OrderBy(x => x.OccurredAt)
            .Take(take)
            .ToListAsync();

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
