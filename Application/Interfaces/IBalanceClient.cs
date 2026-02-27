namespace OrderApi.Application.Interfaces;

public interface IBalanceClient
{
    Task ReserveAsync(Guid orderId, Guid userId, decimal amount);
    Task CompensateAsync(Guid orderId, bool success);
}
