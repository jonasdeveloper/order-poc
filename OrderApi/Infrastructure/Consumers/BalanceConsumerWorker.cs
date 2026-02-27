using System.Text.Json;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using Serilog;
using OrderApi.Application.Events;
using OrderApi.Application.Interfaces;
using OrderApi.Infrastructure.Messaging;
using OrderApi.Infrastructure.Persistence;

namespace OrderApi.Infrastructure.Consumers;

public class BalanceConsumerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAmazonSQS _sqs;
    private readonly AwsOptions _opt;
    private string? _queueUrl;

    public BalanceConsumerWorker(IServiceScopeFactory scopeFactory, IOptions<AwsOptions> opt)
    {
        _scopeFactory = scopeFactory;
        _opt = opt.Value;

        var cfg = new AmazonSQSConfig
        {
            ServiceURL = _opt.SqsEndpoint,
            AuthenticationRegion = _opt.Region
        };

        _sqs = new AmazonSQSClient(new BasicAWSCredentials("test", "test"), cfg);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var urlResp = await _sqs.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = _opt.BalanceQueueName }, stoppingToken);
        _queueUrl = urlResp.QueueUrl;

        Log.Information("BalanceConsumer connected to queue {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            var received = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 5,
                WaitTimeSeconds = 10
            }, stoppingToken);

            var msgs = received.Messages ?? new List<Message>();
            if (msgs.Count == 0) continue;

            foreach (var msg in msgs)
            {
                if (msg == null || string.IsNullOrWhiteSpace(msg.Body) || string.IsNullOrWhiteSpace(msg.ReceiptHandle))
                    continue;

                using var scope = _scopeFactory.CreateScope();
                var balance = scope.ServiceProvider.GetRequiredService<IBalanceClient>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                try
                {
                    var evt = JsonSerializer.Deserialize<OrderSettledEvent>(msg.Body);
                    if (evt == null) throw new Exception("Invalid OrderSettledEvent");

                    await balance.CompensateAsync(evt.OrderId, evt.Success);
                    await uow.CommitAsync();

                    await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);

                    Log.Information("Balance compensation done order_id={OrderId} success={Success}", evt.OrderId, evt.Success);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed processing balance message");
                }
            }
        }
    }
}
