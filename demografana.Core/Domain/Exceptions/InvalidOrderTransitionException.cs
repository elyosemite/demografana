public class InvalidOrderTransitionException(OrderStatus from, OrderStatus to)
    : Exception($"Cannot transition Order from '{from}' to '{to}'.")
{
    public OrderStatus From { get; } = from;
    public OrderStatus To { get; } = to;
}
