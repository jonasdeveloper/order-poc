namespace OrderApi.Application.Events;

public record OrderCreatedEvent(Guid OrderId, Guid UserId, decimal Amount, string Asset, string Type);
