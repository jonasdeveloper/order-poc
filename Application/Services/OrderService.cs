using System.Text.Json;
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
    private readonly IOrderRepository _orderRepository;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISessionService _session;
    private readonly IAntiFraudClient _antiFraud;
    private readonly IBalanceClient _balance;

    public OrderService(IOrderRepository orderRepository, IIdempotencyRepository idempotencyRepository,
        IUnitOfWork unitOfWork, ISessionService session, IAntiFraudClient antiFraud, IBalanceClient balance,
        IOutboxRepository outboxRepository)
    {
        _orderRepository = orderRepository;
        _idempotencyRepository = idempotencyRepository;
        _unitOfWork = unitOfWork;
        _session = session;
        _antiFraud = antiFraud;
        _balance = balance;
        _outboxRepository = outboxRepository;
    }

    public async Task<OrderResponseDTO> CreateAsync(OrderRequestDTO request, string idempotencyKey, string bearerToken)
    {
        ValidHeader(request, idempotencyKey, bearerToken);

        var existingOrderId = await _idempotencyRepository.GetByKeyAsync(idempotencyKey);

        if (existingOrderId != null)
            return await GetExistingOrderAsync(existingOrderId.OrderId);

        var userId = await GetUserId(bearerToken);

        await VerifyFraud(request, userId);

        await ReserveAmount(request, userId);

        return await ProcessNewOrderAsync(request, idempotencyKey, userId);
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
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null) return null;
        var orderResponse = new OrderResponseDTO(order.Id, order.UserId, order.Amount, order.Asset, order.Type, order.Status);
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

        var orderResponseDto = new OrderResponseDTO(order.Id,  order.UserId, order.Amount, order.Asset, order.Type, order.Status);
        Log.Information("Idempotent request - returning existing order {OrderId} with status {Status}",
            orderResponseDto.Id, orderResponseDto.Status);
        return orderResponseDto;
    }
    
    private async Task VerifyFraud(OrderRequestDTO request, Guid userId)
    {
        var isFraud = await _antiFraud.IsFraudAsync(userId, request.Amount);
        if (isFraud)
        {
            Log.Warning("Fraud detected for user_id={UserId} amount={Amount}", userId, request.Amount);
            throw new FraudException();
        }
    }
    
    private async Task ReserveAmount(OrderRequestDTO request, Guid userId)
    {
        await _balance.ReserveAsync(userId, request.Amount);
        Log.Information("Balance reserved for user_id={UserId} amount={Amount}", userId, request.Amount);
    }

    private async Task<Guid> GetUserId(string bearerToken)
    {
        var userId = await _session.GetUserIdAsync(bearerToken);
        Log.Information("Resolved user_id={UserId}", userId);
        return userId;
    }

    private async Task<OrderResponseDTO> ProcessNewOrderAsync(OrderRequestDTO request, string idempotencyKey, Guid userId)
    {
        var order = new Order(request, userId);
        await _orderRepository.AddAsync(order);
        await _idempotencyRepository.AddAsync(new IdempotencyRecord(idempotencyKey, order.Id));

        var orderResponse = new OrderResponseDTO(order.Id, order.UserId, order.Amount, order.Asset, order.Type, order.Status);

        var payload = JsonSerializer.Serialize(orderResponse);
        
        await _outboxRepository.AddAsync(new OutboxEvent("OrderCreated", payload));

        await _unitOfWork.CommitAsync();
        
        Log.Information("New order created with id {OrderId} for idempotency key {IdempotencyKey} and outbox register success", orderResponse.Id,
            idempotencyKey);
        return orderResponse;
    }
}