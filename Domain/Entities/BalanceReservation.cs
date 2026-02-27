namespace OrderApi.Domain.Entities;

public class BalanceReservation
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Status { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private BalanceReservation() { }

    public BalanceReservation(Guid orderId, Guid userId, decimal amount)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        UserId = userId;
        Amount = amount;
        Status = "RESERVED";
        CreatedAt = DateTime.UtcNow;
    }

    public void Confirm() => Status = "CONFIRMED";
    public void Release() => Status = "RELEASED";
}
