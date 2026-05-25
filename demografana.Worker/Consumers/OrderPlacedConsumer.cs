using MassTransit;
using Microsoft.EntityFrameworkCore;

public class OrderPlacedConsumer(AppDbContext db, ILogger<OrderPlacedConsumer> logger)
    : IConsumer<OrderPlaced>
{
    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var e = context.Message;

        var exists = await db.Orders.AnyAsync(o => o.Id == e.OrderId, context.CancellationToken);
        if (exists) return;

        db.Orders.Add(new OrderProjection
        {
            Id = e.OrderId,
            CustomerId = e.CustomerId,
            Items = e.Items,
            Total = e.Total,
            Status = OrderStatus.Pending.ToString(),
            PlacedAt = e.OccurredAt
        });

        await MarkProjectedAsync(e.OrderId, e.Version, context.CancellationToken);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Projected OrderPlaced for Order {OrderId}", e.OrderId);
    }

    private async Task MarkProjectedAsync(Guid aggregateId, int version, CancellationToken ct)
    {
        var record = await db.OrderEvents
            .FirstOrDefaultAsync(r => r.AggregateId == aggregateId && r.Version == version, ct);
        if (record is not null)
            record.ProjectedAt = DateTimeOffset.UtcNow;
    }
}
