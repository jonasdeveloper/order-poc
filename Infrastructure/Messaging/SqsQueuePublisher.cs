using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using OrderApi.Application.Interfaces;

namespace OrderApi.Infrastructure.Messaging;

public class AwsOptions
{
    public string Region { get; set; } = "sa-east-1";
    public string SqsEndpoint { get; set; } = "http://localhost:4566";
    public string OrderQueueName { get; set; } = "order-queue";
}

public class SqsQueuePublisher : IQueuePublisher
{
    private readonly IAmazonSQS _sqs;
    private readonly AwsOptions _opt;
    private string? _orderQueueUrl;

    public SqsQueuePublisher(IOptions<AwsOptions> options)
    {
        _opt = options.Value;

        var cfg = new AmazonSQSConfig
        {
            ServiceURL = _opt.SqsEndpoint,
            AuthenticationRegion = _opt.Region
        };

        _sqs = new AmazonSQSClient(new BasicAWSCredentials("test", "test"), cfg);
    }

    public async Task PublishToOrderQueueAsync(string messageBody, CancellationToken ct)
    {
        var queueUrl = await GetOrderQueueUrlAsync(ct);

        await _sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = messageBody
        }, ct);
    }

    private async Task<string> GetOrderQueueUrlAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_orderQueueUrl))
            return _orderQueueUrl;

        var resp = await _sqs.GetQueueUrlAsync(new GetQueueUrlRequest
        {
            QueueName = _opt.OrderQueueName
        }, ct);

        _orderQueueUrl = resp.QueueUrl;
        return _orderQueueUrl;
    }
}
