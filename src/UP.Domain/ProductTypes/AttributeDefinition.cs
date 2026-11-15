namespace UP.Domain.ProductTypes;

public sealed record AttributeDefinition(
    string Key,
    string Label,
    AttributeKind Kind,
    bool Required,
    int? MaxLength = null,
    int? Min = null);
