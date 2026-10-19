using UP.Domain.Common;

namespace UP.Domain.Products;

public sealed class Product
{
    public Guid Id { get; set; }

    public ProductType Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public required Money Price { get; set; }

    public string Attributes { get; set; } = "{}";
}
