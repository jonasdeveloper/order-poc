namespace OrderApi.Domain.Entities;

public class IdempotencyRecord
{
    public Guid Id { get; private set; }
    public string Key { get; private set; } = default!;
    public Guid OrderId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private IdempotencyRecord() { }

    public IdempotencyRecord(string key, Guid orderId)
    {
        Id = Guid.NewGuid();
        Key = key;
        OrderId = orderId;
        CreatedAt = DateTime.UtcNow;
    }
}
