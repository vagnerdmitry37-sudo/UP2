using UP.Domain.ProductTypes;

namespace UP.Api.ProductTypes;

public sealed record AttributeDefinitionResponse(
    string Key,
    string Label,
    AttributeKind Kind,
    bool Required,
    int? MaxLength,
    int? Min);
