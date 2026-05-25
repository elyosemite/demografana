namespace Demografana.Core.Domain.Events;

public record OrderDelivered(
    Guid OrderId,
    int Version,
    DateTimeOffset OccurredAt) : OrderEvent(OrderId, Version, OccurredAt);
