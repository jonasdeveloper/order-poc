using OrderApi.Domain.Entities;

namespace OrderApi.Application.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task<Order?> GetByIdAsync(Guid id);
    Task SaveChangesAsync();
}
