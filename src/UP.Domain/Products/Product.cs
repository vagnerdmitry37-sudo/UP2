namespace UP.Domain.Products;

public sealed class Product
{
    public Guid Id { get; set; }

    public ProductType Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public string SeoDescription { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Attributes { get; set; } = "{}";
}
