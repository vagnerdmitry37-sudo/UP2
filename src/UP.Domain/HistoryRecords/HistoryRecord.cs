namespace UP.Domain.HistoryRecords;

public sealed class HistoryRecord
{
    public Guid Id { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public HistoryOperation Operation { get; set; }

    public Guid? UserId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string Changes { get; set; } = "{}";
}
