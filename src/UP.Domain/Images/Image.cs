namespace UP.Domain.Images;

public sealed class Image
{
    public Guid Id { get; set; }

    public ImageEntityType EntityType { get; set; }

    public Guid EntityId { get; set; }

    public string Key { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
