using System.Text.Json;
using OrderApi.Application.Events;
using OrderApi.Application.Interfaces;
using OrderApi.Application.Interfaces.Application.Interfaces;
using OrderApi.Domain.Entities;
using OrderApi.Domain.Exceptions;
using OrderApi.DTO;
using OrderApi.Infrastructure.Persistence;
using Serilog;

namespace OrderApi.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly IIdempotencyRepository _idem;
    private readonly ISessionService _session;
    private readonly IAntiFraudClient _antiFraud;
    private readonly IBalanceClient _balance;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _uow;

    public OrderService(
        IOrderRepository orders,
        IIdempotencyRepository idem,
        ISessionService session,
        IAntiFraudClient antiFraud,
        IBalanceClient balance,
        IOutboxWriter outboxWriter,
        IUnitOfWork uow)
    {
        _orders = orders;
        _idem = idem;
        _session = session;
        _antiFraud = antiFraud;
        _balance = balance;
        _outboxWriter = outboxWriter;
        _uow = uow;
    }

    public async Task<OrderResponseDTO> CreateAsync(OrderRequestDTO request, string idempotencyKey, string bearerToken)
    {
        ValidHeader(request, idempotencyKey, bearerToken);

        var existing = await _idem.GetByKeyAsync(idempotencyKey);
        if (existing != null)
            return await GetExistingOrderAsync(existing.OrderId);

        var userId = await _session.GetUserIdAsync(bearerToken);

        var isFraud = await _antiFraud.IsFraudAsync(userId, request.Amount);
        if (isFraud)
            throw new FraudException();

        await _balance.ReserveAsync(userId, request.Amount);

        var order = new Order(request, userId);
        await _orders.AddAsync(order);

        await _idem.AddAsync(new IdempotencyRecord(idempotencyKey, order.Id));

        await _outboxWriter.EnqueueAsync(
            type: "OrderCreated",
            payload: new OrderCreatedEvent(order.Id, order.UserId, order.Amount, order.Asset, order.Type)
        );

        await _uow.CommitAsync();

        Log.Information("Order created order_id={OrderId} user_id={UserId}", order.Id, userId);

        return new OrderResponseDTO(order.Id, order.UserId, order.Amount, order.Asset, order.Type, order.Status);
    }

    private static void ValidHeader(OrderRequestDTO? request, string idempotencyKey, string bearerToken)
    {
        if (request is null)
        {
            Log.Error("Request body is null");
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            Log.Error("Missing idempotency key in request header");
            throw new ArgumentException("Missing idempotency key");
        }

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            Log.Error("Missing bearer token in request header");
            throw new UnauthorizedAccessException("Missing bearer token");
        }
    }

    public async Task<OrderResponseDTO?> GetByIdAsync(Guid id)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order == null) return null;
        var orderResponse = new OrderResponseDTO(order.Id, order.UserId, order.Amount, order.Asset, order.Type, order.Status);
        return orderResponse;
    }

    private async Task<OrderResponseDTO> GetExistingOrderAsync(Guid orderId)
    {
        var order = await _orders.GetByIdAsync(orderId);

        if (order == null)
        {
            Log.Error("Idempotency record exists for Order {OrderId}, but order was not found.", orderId);
            throw new Exception($"Idempotency record exists for Order {orderId}, but order was not found.");
        }

        var orderResponseDto = new OrderResponseDTO(order.Id,  order.UserId, order.Amount, order.Asset, order.Type, order.Status);
        Log.Information("Idempotent request - returning existing order {OrderId} with status {Status}",
            orderResponseDto.Id, orderResponseDto.Status);
        return orderResponseDto;
    }
}
