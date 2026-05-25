namespace Demografana.Core.Domain.Events;

public record OrderPlaced(
    Guid OrderId,
    int Version,
    DateTimeOffset OccurredAt,
    string CustomerId,
    List<OrderItem> Items,
    decimal Total) : OrderEvent(OrderId, Version, OccurredAt);
