using OrderApi.Domain.Entities;

namespace OrderApi.Application.Interfaces;

public interface IIdempotencyRepository
{
    Task<IdempotencyRecord?> GetByKeyAsync(string key);
    Task AddAsync(IdempotencyRecord record);
    Task SaveChangesAsync();
}
