sealed class DeliverOrderUseCase(EventStore store, ILogger<DeliverOrderUseCase> logger)
{
    public async Task ExecuteAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await store.LoadAsync(orderId, ct);
        order.Deliver();
        await store.AppendAsync(order, ct);
        logger.LogInformation("Order {OrderId} delivered", orderId);
    }
}
