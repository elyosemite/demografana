using Demografana.Core.Domain.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

public class OrderDeliveredConsumer(AppDbContext db, ILogger<OrderDeliveredConsumer> logger)
    : IConsumer<OrderDelivered>
{
    public async Task Consume(ConsumeContext<OrderDelivered> context)
    {
        var e = context.Message;
        var projection = await db.Orders.FindAsync([e.OrderId], context.CancellationToken);
        if (projection is null) return;

        projection.Status = OrderStatus.Delivered.ToString();
        projection.DeliveredAt = e.OccurredAt;

        await MarkProjectedAsync(e.OrderId, e.Version, context.CancellationToken);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Projected OrderDelivered for Order {OrderId}", e.OrderId);
    }

    private async Task MarkProjectedAsync(Guid aggregateId, int version, CancellationToken ct)
    {
        var record = await db.OrderEvents
            .FirstOrDefaultAsync(r => r.AggregateId == aggregateId && r.Version == version, ct);
        if (record is not null)
            record.ProjectedAt = DateTimeOffset.UtcNow;
    }
}
