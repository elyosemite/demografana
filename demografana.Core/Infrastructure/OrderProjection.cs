public class OrderProjection
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = [];
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset PlacedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
