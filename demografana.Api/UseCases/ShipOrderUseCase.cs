sealed class ShipOrderUseCase(EventStore store, ILogger<ShipOrderUseCase> logger)
{
    public async Task ExecuteAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await store.LoadAsync(orderId, ct);
        order.Ship();
        await store.AppendAsync(order, ct);
        logger.LogInformation("Order {OrderId} shipped", orderId);
    }
}
