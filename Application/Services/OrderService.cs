using OrderApi.Application.Interfaces;
using OrderApi.Domain.Entities;
using OrderApi.DTO;
using OrderApi.Infrastructure.Persistence;

namespace OrderApi.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<OrderResponseDTO> CreateAsync(OrderRequestDTO request)
    {
        var order = new Order(request);
        await _repository.AddAsync(order);
        await _repository.SaveChangesAsync();
        return new OrderResponseDTO(order.Id, order.Amount, order.Asset, order.Type);
    }

    public async Task<OrderResponseDTO?> GetByIdAsync(Guid id)
    {
        var order =  await _repository.GetByIdAsync(id);
        if (order == null) return null;
        var orderResponse = new OrderResponseDTO(order.Id, order.Amount, order.Asset, order.Type);
        return orderResponse;
    }
}