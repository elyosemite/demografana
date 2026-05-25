using MassTransit;
using Microsoft.EntityFrameworkCore;

public class OrderCancelledConsumer(AppDbContext db, ILogger<OrderCancelledConsumer> logger)
    : IConsumer<OrderCancelled>
{
    public async Task Consume(ConsumeContext<OrderCancelled> context)
    {
        var e = context.Message;
        var projection = await db.Orders.FindAsync([e.OrderId], context.CancellationToken);
        if (projection is null) return;

        projection.Status = OrderStatus.Cancelled.ToString();
        projection.CancelledAt = e.OccurredAt;
        projection.CancelReason = e.Reason;

        await MarkProjectedAsync(e.OrderId, e.Version, context.CancellationToken);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Projected OrderCancelled for Order {OrderId}. Reason: {Reason}", e.OrderId, e.Reason);
    }

    private async Task MarkProjectedAsync(Guid aggregateId, int version, CancellationToken ct)
    {
        var record = await db.OrderEvents
            .FirstOrDefaultAsync(r => r.AggregateId == aggregateId && r.Version == version, ct);
        if (record is not null)
            record.ProjectedAt = DateTimeOffset.UtcNow;
    }
}
