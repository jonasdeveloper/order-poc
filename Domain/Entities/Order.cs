namespace OrderApi.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    public string Status { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private Order() { }

    public Order(decimal amount)
    {
        Id = Guid.NewGuid();
        Amount = amount;
        Status = "PENDING";
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessed()
    {
        Status = "PROCESSED";
    }

    public void MarkAsFailed()
    {
        Status = "FAILED";
    }
}
