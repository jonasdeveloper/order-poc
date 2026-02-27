namespace OrderApi.Application.Interfaces.Application.Interfaces;

public interface IAntiFraudClient
{
    Task<bool> IsFraudAsync(Guid userId, decimal amount);
}
