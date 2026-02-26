using System.Text.Json;
using OrderApi.Application.Interfaces;
using OrderApi.Domain.Entities;

namespace OrderApi.Infrastructure.Outbox;

public class OutboxWriter : IOutboxWriter
{
    private readonly IOutboxRepository _outbox;

    public OutboxWriter(IOutboxRepository outbox)
    {
        _outbox = outbox;
    }

    public async Task EnqueueAsync(string type, object payload)
    {
        var json = JsonSerializer.Serialize(payload);

        await _outbox.AddAsync(new OutboxEvent(
            type: type,
            payload: json
        ));
    }
}
