public record OrderShipped(
    Guid OrderId,
    int Version,
    DateTimeOffset OccurredAt) : OrderEvent(OrderId, Version, OccurredAt);
