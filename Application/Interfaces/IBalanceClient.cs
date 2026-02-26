namespace OrderApi.Application.Interfaces;

public interface IBalanceClient
{
    Task ReserveAsync(Guid userId, decimal amount);
}
