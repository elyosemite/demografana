sealed class GetOrdersUseCase(OrderStore store)
{
    public IReadOnlyList<Order> Execute() => store.All();
}
