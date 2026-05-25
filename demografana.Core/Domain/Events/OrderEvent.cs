namespace Demografana.Core.Domain.Events;

public abstract record OrderEvent(Guid OrderId, int Version, DateTimeOffset OccurredAt);
