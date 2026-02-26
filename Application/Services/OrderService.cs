using OrderApi.Application.Interfaces;
using OrderApi.Domain.Entities;
using OrderApi.DTO;
using OrderApi.Infrastructure.Persistence;
using Serilog;

namespace OrderApi.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IOrderRepository orderRepository, IIdempotencyRepository idempotencyRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _idempotencyRepository = idempotencyRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<OrderResponseDTO> CreateAsync(OrderRequestDTO request, string idempotencyKey)
    {
        try
        {
            var existingOrderId = await _idempotencyRepository.GetByKeyAsync(idempotencyKey);

            if (existingOrderId != null)
            {
                return await GetExistingOrderAsync(existingOrderId.OrderId);
            }

            return await ProcessNewOrderAsync(request, idempotencyKey);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while processing order request");
            throw new Exception($"Error processing order with idempotency key {idempotencyKey}: {ex.Message}", ex);
        }
    }

    public async Task<OrderResponseDTO?> GetByIdAsync(Guid id)
    {
        var order =  await _orderRepository.GetByIdAsync(id);
        if (order == null) return null;
        var orderResponse = new OrderResponseDTO(order.Id, order.Amount, order.Asset, order.Type, order.Status);
        return orderResponse;
    }
    private async Task<OrderResponseDTO> GetExistingOrderAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);

        if (order == null)
        {
            Log.Error("Idempotency record exists for Order {OrderId}, but order was not found.", orderId);
            throw new Exception($"Idempotency record exists for Order {orderId}, but order was not found.");
        }

        var orderResponseDto = new OrderResponseDTO(order.Id, order.Amount, order.Asset, order.Type, order.Status);
        Log.Information("Idempotent request - returning existing order {OrderId} with status {Status}", orderResponseDto.Id, orderResponseDto.Status);
        return orderResponseDto;
    }

    private async Task<OrderResponseDTO> ProcessNewOrderAsync(OrderRequestDTO request, string idempotencyKey)
    {
        var order = new Order(request); 
    
        await _orderRepository.AddAsync(order);
        await _idempotencyRepository.AddAsync(new IdempotencyRecord(idempotencyKey, order.Id));
        await _unitOfWork.CommitAsync(); 

        var orderResponse = new OrderResponseDTO(order.Id, order.Amount, order.Asset, order.Type, order.Status);
        Log.Information("New order created with id {OrderId} for idempotency key {IdempotencyKey}", orderResponse.Id, idempotencyKey);
        return orderResponse;
    }
}