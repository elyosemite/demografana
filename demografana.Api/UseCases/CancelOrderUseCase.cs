sealed class CancelOrderUseCase(EventStore store, ILogger<CancelOrderUseCase> logger)
{
    public async Task ExecuteAsync(Guid orderId, string reason, CancellationToken ct = default)
    {
        var order = await store.LoadAsync(orderId, ct);
        order.Cancel(reason);
        await store.AppendAsync(order, ct);
        logger.LogInformation("Order {OrderId} cancelled. Reason: {Reason}", orderId, reason);
    }
}

record CancelOrderRequest(string Reason);
