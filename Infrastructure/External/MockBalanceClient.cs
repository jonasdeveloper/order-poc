using OrderApi.Application.Interfaces;
using OrderApi.Domain.Exceptions;
using Serilog;

namespace OrderApi.Infrastructure.External;

public class MockBalanceClient : IBalanceClient
{
    public Task ReserveAsync(Guid userId, decimal amount)
    {
        if (amount > 5000m)
        {
            Log.Error("User {UserId} has insufficient balance to reserve {Amount}", userId, amount);
            throw new BalanceException("Insufficient balance");
        }
        return Task.CompletedTask;
    }
}
