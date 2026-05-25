sealed class ConfirmOrderUseCase(EventStore store, ILogger<ConfirmOrderUseCase> logger)
{
    public async Task ExecuteAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await store.LoadAsync(orderId, ct);
        order.Confirm();
        await store.AppendAsync(order, ct);
        logger.LogInformation("Order {OrderId} confirmed", orderId);
    }
}
