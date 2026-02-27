using Microsoft.EntityFrameworkCore;
using OrderApi.Application.Interfaces;
using OrderApi.Domain.Entities;
using OrderApi.Infrastructure.Persistence;

namespace OrderApi.Infrastructure.Repositories;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly OrderDbContext _context;

    public IdempotencyRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<IdempotencyRecord?> GetByKeyAsync(string key)
    {
        return await _context.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == key);
    }

    public async Task AddAsync(IdempotencyRecord record)
    {
        await _context.IdempotencyRecords.AddAsync(record);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
