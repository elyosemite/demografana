sealed class PlaceOrderUseCase(OrderStore store, ILogger<PlaceOrderUseCase> logger)
{
    public Task<Order> ExecuteAsync(PlaceOrderRequest request)
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

        var total = request.Items.Sum(i => i.Quantity * i.UnitPrice);

        var order = new Order(
            Id: Guid.NewGuid(),
            CustomerId: request.CustomerId,
            Items: request.Items,
            Total: total,
            Status: "Pending",
            CreatedAt: DateTimeOffset.UtcNow);

        store.Add(order);

        logger.LogInformation(
            "Order {OrderId} placed for customer {CustomerId} — {ItemCount} item(s), total {Total}",
            order.Id, order.CustomerId, order.Items.Count, order.Total);

        return Task.FromResult(order);
    }
}
