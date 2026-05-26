using Demografana.Core.Domain.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

public class OutboxRelay(IServiceScopeFactory scopeFactory, IBus bus, ILogger<OutboxRelay> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Outbox Relay started");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox batch");
            }

            await Task.Delay(500, ct);
        }

        logger.LogInformation("Outbox Relay stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pending = await db.OrderEvents
            .Where(e => e.PublishedAt == null)
            .OrderBy(e => e.OccurredAt)
            .Take(100)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        logger.LogInformation("Publishing {Count} event(s) to RabbitMQ", pending.Count);

        foreach (var record in pending)
        {
            logger.LogDebug("Publishing {EventType} for order {AggregateId} v{Version}",
                record.EventType, record.AggregateId, record.Version);

            await PublishAsync(record, ct);
            record.PublishedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogDebug("Published {EventType} for order {AggregateId} v{Version}",
                record.EventType, record.AggregateId, record.Version);
        }

        logger.LogInformation("Batch of {Count} event(s) published", pending.Count);
    }

    private async Task PublishAsync(OrderEventRecord record, CancellationToken ct)
    {
        switch (record.EventType)
        {
            case nameof(OrderPlaced):
                await bus.Publish(Deserialize<OrderPlaced>(record), ct);
                break;
            case nameof(OrderConfirmed):
                await bus.Publish(Deserialize<OrderConfirmed>(record), ct);
                break;
            case nameof(OrderShipped):
                await bus.Publish(Deserialize<OrderShipped>(record), ct);
                break;
            case nameof(OrderDelivered):
                await bus.Publish(Deserialize<OrderDelivered>(record), ct);
                break;
            case nameof(OrderCancelled):
                await bus.Publish(Deserialize<OrderCancelled>(record), ct);
                break;
            default:
                logger.LogWarning("Unknown event type {EventType} — skipping", record.EventType);
                break;
        }
    }

    private static T Deserialize<T>(OrderEventRecord record) =>
        System.Text.Json.JsonSerializer.Deserialize<T>(record.Payload,
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })!;
}

