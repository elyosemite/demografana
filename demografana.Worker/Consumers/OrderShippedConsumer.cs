using MassTransit;
using Microsoft.EntityFrameworkCore;

public class OrderShippedConsumer(AppDbContext db, ILogger<OrderShippedConsumer> logger)
    : IConsumer<OrderShipped>
{
    public async Task Consume(ConsumeContext<OrderShipped> context)
    {
        var e = context.Message;
        var projection = await db.Orders.FindAsync([e.OrderId], context.CancellationToken);
        if (projection is null) return;

        projection.Status = OrderStatus.Shipped.ToString();
        projection.ShippedAt = e.OccurredAt;

        await MarkProjectedAsync(e.OrderId, e.Version, context.CancellationToken);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Projected OrderShipped for Order {OrderId}", e.OrderId);
    }

    private async Task MarkProjectedAsync(Guid aggregateId, int version, CancellationToken ct)
    {
        var record = await db.OrderEvents
            .FirstOrDefaultAsync(r => r.AggregateId == aggregateId && r.Version == version, ct);
        if (record is not null)
            record.ProjectedAt = DateTimeOffset.UtcNow;
    }
}
