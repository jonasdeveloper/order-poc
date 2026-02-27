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
    public string BalanceQueueName { get; set; } = "balance-queue";
}

public class SqsQueuePublisher : IQueuePublisher
{
    private readonly IAmazonSQS _sqs;
    private readonly AwsOptions _opt;
    private readonly Dictionary<string, string> _queueUrlCache = new();

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

    public async Task PublishAsync(string queueName, string messageBody, CancellationToken ct)
    {
        var url = await GetQueueUrlAsync(queueName, ct);

        await _sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = url,
            MessageBody = messageBody
        }, ct);
    }

    private async Task<string> GetQueueUrlAsync(string queueName, CancellationToken ct)
    {
        if (_queueUrlCache.TryGetValue(queueName, out var cached))
            return cached;

        var resp = await _sqs.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = queueName }, ct);
        _queueUrlCache[queueName] = resp.QueueUrl;
        return resp.QueueUrl;
    }
}
