public abstract record OrderEvent(Guid OrderId, int Version, DateTimeOffset OccurredAt);
