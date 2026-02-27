using System.Text.Json;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using OrderApi.Application.Events;
using OrderApi.Application.Interfaces;
using OrderApi.Infrastructure.Messaging;
using OrderApi.Infrastructure.Persistence;
using Serilog;

namespace OrderApi.Infrastructure.Consumers;

public class OrderConsumerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAmazonSQS _sqs;
    private readonly AwsOptions _opt;
    private string? _queueUrl;

    public OrderConsumerWorker(IServiceScopeFactory scopeFactory, IOptions<AwsOptions> opt)
    {
        _scopeFactory = scopeFactory;

        _opt = opt?.Value ??
               throw new InvalidOperationException("AwsOptions not configured (IOptions<AwsOptions> is null)");
        if (_opt is null) throw new InvalidOperationException("AwsOptions.Value is null");

        var cfg = new AmazonSQSConfig
        {
            ServiceURL = _opt.SqsEndpoint,
            AuthenticationRegion = _opt.Region
        };

        _sqs = new AmazonSQSClient(new BasicAWSCredentials("test", "test"), cfg);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_opt.SqsEndpoint) ||
            string.IsNullOrWhiteSpace(_opt.Region) ||
            string.IsNullOrWhiteSpace(_opt.OrderQueueName))
        {
            throw new InvalidOperationException("Aws configuration missing: Region/SqsEndpoint/OrderQueueName");
        }

        var urlResp = await _sqs.GetQueueUrlAsync(new GetQueueUrlRequest
        {
            QueueName = _opt.OrderQueueName
        }, stoppingToken);

        _queueUrl = urlResp.QueueUrl;

        Log.Information("OrderConsumer connected to queue {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            var received = await ReceiveMessageResponse(stoppingToken);
            var msgs = received.Messages ?? new List<Message>();
            
            if (msgs.Count == 0)
                continue;

            foreach (var msg in msgs)
            {
                if (msg == null || string.IsNullOrWhiteSpace(msg.Body) || string.IsNullOrWhiteSpace(msg.ReceiptHandle))
                {
                    Log.Warning("Skipping invalid SQS message (null/body/receipt).");
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOrderProcessor>();
                var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var publisher = scope.ServiceProvider.GetRequiredService<IQueuePublisher>();
                var aws = scope.ServiceProvider.GetRequiredService<IOptions<AwsOptions>>().Value;

                try
                {
                    var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(msg.Body);
                    if (evt == null)
                        throw new Exception("Invalid message body: cannot deserialize OrderCreatedEvent");

                    await processor.ProcessAsync(evt.OrderId, stoppingToken);
                    var order = await orderRepo.GetByIdForUpdateAsync(evt.OrderId);

                    if (order == null)
                    {
                        Log.Error("Order not found in DB for order_id={OrderId}", evt.OrderId);
                        continue;
                    }

                    order.MarkAsProcessed();
                    await uow.CommitAsync();
                    await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                    Log.Information("Order {OrderId} processed successfully new state {Status}", evt.OrderId, order.Status);

                    await publisher.PublishAsync(
                        aws.BalanceQueueName,
                        System.Text.Json.JsonSerializer.Serialize(new OrderSettledEvent(evt.OrderId, true)),
                        stoppingToken
                    );

                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed processing order message: {MessageId}", msg.MessageId);
                }
            }
        }
    }

    private async Task<ReceiveMessageResponse> ReceiveMessageResponse(CancellationToken stoppingToken)
    {
        var received = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _queueUrl,
            MaxNumberOfMessages = 5,
            WaitTimeSeconds = 10
        }, stoppingToken);
        return received;
    }
}