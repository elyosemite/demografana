public record OrderConfirmed(
    Guid OrderId,
    int Version,
    DateTimeOffset OccurredAt) : OrderEvent(OrderId, Version, OccurredAt);
