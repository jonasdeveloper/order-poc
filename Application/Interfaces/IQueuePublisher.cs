namespace OrderApi.Application.Interfaces;

public interface IQueuePublisher
{
    Task PublishToOrderQueueAsync(string messageBody, CancellationToken ct);
}
