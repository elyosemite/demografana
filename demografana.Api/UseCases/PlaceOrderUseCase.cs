sealed class PlaceOrderUseCase(EventStore store, ILogger<PlaceOrderUseCase> logger)
{
    public async Task<OrderProjection?> ExecuteAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Placing order for customer {CustomerId} with {ItemCount} item(s)",
            request.CustomerId, request.Items.Count);

        if (request.Items.Count == 0)
        {
            logger.LogWarning(
                "Order rejected for customer {CustomerId}: no items provided",
                request.CustomerId);
            throw new ArgumentException("An order must have at least one item.");
        }

        var order = Order.Place(Guid.NewGuid(), request.CustomerId, request.Items);
        await store.AppendAsync(order, ct);

        logger.LogInformation(
            "Order {OrderId} placed for customer {CustomerId} — {ItemCount} item(s), total {Total}",
            order.Id, order.CustomerId, order.Items.Count, order.Total);

        return new OrderProjection
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Items = order.Items,
            Total = order.Total,
            Status = order.Status.ToString(),
            PlacedAt = order.CreatedAt
        };
    }
}

record PlaceOrderRequest(string CustomerId, List<OrderItem> Items);
