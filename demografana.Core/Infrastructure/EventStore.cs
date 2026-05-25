using Demografana.Core.Domain.Events;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public class EventStore(AppDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Order> LoadAsync(Guid orderId, CancellationToken ct = default)
    {
        var records = await db.OrderEvents
            .Where(e => e.AggregateId == orderId)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        if (records.Count == 0)
            throw new OrderNotFoundException(orderId);

        return Order.Load(records.Select(Deserialize));
    }

    public async Task AppendAsync(Order order, CancellationToken ct = default)
    {
        foreach (var e in order.PendingEvents)
        {
            db.OrderEvents.Add(new OrderEventRecord
            {
                Id = Guid.NewGuid(),
                AggregateId = e.OrderId,
                Version = e.Version,
                EventType = e.GetType().Name,
                Payload = JsonSerializer.Serialize(e, e.GetType(), JsonOptions),
                OccurredAt = e.OccurredAt
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private static OrderEvent Deserialize(OrderEventRecord record) => record.EventType switch
    {
        nameof(OrderPlaced)    => JsonSerializer.Deserialize<OrderPlaced>(record.Payload, JsonOptions)!,
        nameof(OrderConfirmed) => JsonSerializer.Deserialize<OrderConfirmed>(record.Payload, JsonOptions)!,
        nameof(OrderShipped)   => JsonSerializer.Deserialize<OrderShipped>(record.Payload, JsonOptions)!,
        nameof(OrderDelivered) => JsonSerializer.Deserialize<OrderDelivered>(record.Payload, JsonOptions)!,
        nameof(OrderCancelled) => JsonSerializer.Deserialize<OrderCancelled>(record.Payload, JsonOptions)!,
        _ => throw new InvalidOperationException($"Unknown event type: {record.EventType}")
    };
}
