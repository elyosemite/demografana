public class OrderEventRecord
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public int Version { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ProjectedAt { get; set; }
}
