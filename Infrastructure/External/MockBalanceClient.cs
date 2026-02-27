using Serilog;
using OrderApi.Application.Interfaces;
using OrderApi.Domain.Entities;
using OrderApi.Domain.Exceptions;

namespace OrderApi.Infrastructure.External;

public class MockBalanceClient : IBalanceClient
{
    private readonly IBalanceReservationRepository _repo;

    public MockBalanceClient(IBalanceReservationRepository repo)
    {
        _repo = repo;
    }

    public async Task ReserveAsync(Guid orderId, Guid userId, decimal amount)
    {
        if (amount > 5000m)
            throw new InsufficientBalanceException();

        await _repo.AddAsync(new BalanceReservation(orderId, userId, amount));
        Log.Information("Balance RESERVED order_id={OrderId} user_id={UserId} amount={Amount}", orderId, userId, amount);
    }

    public async Task CompensateAsync(Guid orderId, bool success)
    {
        var r = await _repo.GetByOrderIdForUpdateAsync(orderId);
        if (r == null)
        {
            Log.Warning("No reservation found for order_id={OrderId}", orderId);
            return;
        }

        if (success) r.Confirm();
        else r.Release();

        Log.Information("Balance COMPENSATED order_id={OrderId} success={Success} new_status={Status}",
            orderId, success, r.Status);
    }
}
