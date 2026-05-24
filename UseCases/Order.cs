record Order(
    Guid Id,
    string CustomerId,
    List<OrderItem> Items,
    decimal Total,
    string Status,
    DateTimeOffset CreatedAt);

record OrderItem(string ProductId, int Quantity, decimal UnitPrice);

record PlaceOrderRequest(string CustomerId, List<OrderItem> Items);

sealed class OrderStore
{
    private readonly List<Order> _orders = [];

    public IReadOnlyList<Order> All() => _orders.AsReadOnly();

    public void Add(Order order) => _orders.Add(order);
}
