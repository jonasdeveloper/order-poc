using OrderApi.Application.Interfaces.Application.Interfaces;

namespace OrderApi.Infrastructure.External;

public class MockAntiFraudClient : IAntiFraudClient
{
    public Task<bool> IsFraudAsync(Guid userId, decimal amount)
    {
        return Task.FromResult(amount > 10000m);
    }
}
