namespace OrderApi.Domain.Entities;

public class OutboxEvent
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTime OccurredAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string Status { get; private set; } = default!;
    public int AttemptCount { get; private set; }

    private OutboxEvent() { }

    public OutboxEvent(string type, string payload)
    {
        Id = Guid.NewGuid();
        Type = type;
        Payload = payload;
        OccurredAt = DateTime.UtcNow;
        Status = "PENDING";
        AttemptCount = 0;
    }

    public void MarkProcessed()
    {
        Status = "PROCESSED";
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = "FAILED";
        AttemptCount++;
    }
}
