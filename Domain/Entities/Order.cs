using OrderApi.Controllers;
using OrderApi.DTO;

namespace OrderApi.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    
    public string Status { get; private set; } = default!;
    
    public string Type { get; private set; }
    
    public string Asset { get; private set; } = default!;
    
    public DateTime CreatedAt { get; private set; }

    private Order() { }

    public Order(OrderRequestDTO orderRequestRequest)
    {
        Id = Guid.NewGuid();
        Amount = orderRequestRequest.Amount;
        Type = orderRequestRequest.Type;
        Asset = orderRequestRequest.Asset;
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
