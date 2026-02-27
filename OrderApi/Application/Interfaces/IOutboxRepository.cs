using OrderApi.Domain.Entities;

namespace OrderApi.Application.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxEvent evt);
    Task<List<OutboxEvent>> GetPendingAsync(int take);
    Task SaveChangesAsync();
}
