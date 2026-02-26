using OrderApi.Application.Interfaces;
using Serilog;

namespace OrderApi.Infrastructure.Outbox;

public class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public OutboxPublisherWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var publisher = scope.ServiceProvider.GetRequiredService<IQueuePublisher>();

            var pending = await outboxRepo.GetPendingAsync(take: 10);

            foreach (var evt in pending)
            {
                try
                {
                    Log.Information("Publishing outbox event id={OutboxId} type={Type}", evt.Id, evt.Type);

                    await publisher.PublishToOrderQueueAsync(evt.Payload, stoppingToken);

                    evt.MarkProcessed();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed publishing outbox event id={OutboxId}", evt.Id);
                    evt.MarkFailed();
                }
            }

            await outboxRepo.SaveChangesAsync();
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
