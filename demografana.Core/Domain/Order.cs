public class Order
{
    public Guid Id { get; private set; }
    public string CustomerId { get; private set; } = string.Empty;
    public List<OrderItem> Items { get; private set; } = [];
    public decimal Total { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private int _version;
    private readonly List<OrderEvent> _pendingEvents = [];
    public IReadOnlyList<OrderEvent> PendingEvents => _pendingEvents.AsReadOnly();

    private Order() { }

    public static Order Place(Guid id, string customerId, List<OrderItem> items)
    {
        if (items.Count == 0)
            throw new ArgumentException("An order must have at least one item.");

        var total = items.Sum(i => i.Quantity * i.UnitPrice);
        var order = new Order();
        order.Apply(new OrderPlaced(id, 0, DateTimeOffset.UtcNow, customerId, items, total), isNew: true);
        return order;
    }

    public static Order Load(IEnumerable<OrderEvent> events)
    {
        var order = new Order();
        foreach (var e in events)
            order.Apply(e, isNew: false);
        return order;
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOrderTransitionException(Status, OrderStatus.Confirmed);
        Apply(new OrderConfirmed(Id, _version + 1, DateTimeOffset.UtcNow), isNew: true);
    }

    public void Ship()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOrderTransitionException(Status, OrderStatus.Shipped);
        Apply(new OrderShipped(Id, _version + 1, DateTimeOffset.UtcNow), isNew: true);
    }

    public void Deliver()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOrderTransitionException(Status, OrderStatus.Delivered);
        Apply(new OrderDelivered(Id, _version + 1, DateTimeOffset.UtcNow), isNew: true);
    }

    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new InvalidOrderTransitionException(Status, OrderStatus.Cancelled);
        Apply(new OrderCancelled(Id, _version + 1, DateTimeOffset.UtcNow, reason), isNew: true);
    }

    private void Apply(OrderEvent e, bool isNew)
    {
        switch (e)
        {
            case OrderPlaced p:
                Id = p.OrderId;
                CustomerId = p.CustomerId;
                Items = p.Items;
                Total = p.Total;
                Status = OrderStatus.Pending;
                CreatedAt = p.OccurredAt;
                break;
            case OrderConfirmed:
                Status = OrderStatus.Confirmed;
                break;
            case OrderShipped:
                Status = OrderStatus.Shipped;
                break;
            case OrderDelivered:
                Status = OrderStatus.Delivered;
                break;
            case OrderCancelled:
                Status = OrderStatus.Cancelled;
                break;
        }
        _version = e.Version;
        if (isNew) _pendingEvents.Add(e);
    }
}
