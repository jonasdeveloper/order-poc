namespace OrderApi.Application.Events;

public record OrderSettledEvent(Guid OrderId, bool Success);
