namespace OrderApi.Application.Interfaces;

public interface IOrderProcessor
{
    Task ProcessAsync(Guid orderId, CancellationToken ct);
}
