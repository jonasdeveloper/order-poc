namespace OrderApi.Application.Interfaces;

public interface IQueuePublisher
{
    Task PublishAsync(string queueName, string messageBody, CancellationToken ct);
}
