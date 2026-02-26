using OrderApi.DTO;

namespace OrderApi.Application.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDTO> CreateAsync(OrderRequestDTO order, string idempotencyKey);
    Task<OrderResponseDTO?> GetByIdAsync(Guid id);
}