using MassTransit;
using Microsoft.EntityFrameworkCore;

public class OrderConfirmedConsumer(AppDbContext db, ILogger<OrderConfirmedConsumer> logger)
    : IConsumer<OrderConfirmed>
{
    public async Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        var e = context.Message;
        var projection = await db.Orders.FindAsync([e.OrderId], context.CancellationToken);
        if (projection is null) return;

        projection.Status = OrderStatus.Confirmed.ToString();
        projection.ConfirmedAt = e.OccurredAt;

        await MarkProjectedAsync(e.OrderId, e.Version, context.CancellationToken);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Projected OrderConfirmed for Order {OrderId}", e.OrderId);
    }

    private async Task MarkProjectedAsync(Guid aggregateId, int version, CancellationToken ct)
    {
        var record = await db.OrderEvents
            .FirstOrDefaultAsync(r => r.AggregateId == aggregateId && r.Version == version, ct);
        if (record is not null)
            record.ProjectedAt = DateTimeOffset.UtcNow;
    }
}
