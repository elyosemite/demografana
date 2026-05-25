public record OrderCancelled(
    Guid OrderId,
    int Version,
    DateTimeOffset OccurredAt,
    string Reason) : OrderEvent(OrderId, Version, OccurredAt);
