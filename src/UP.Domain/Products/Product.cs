using System.Text.Json;

using UP.Domain.Common;

namespace UP.Domain.Products;

public sealed class Product
{
    public const int MaxTypeLength = 50;
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 5000;
    public const int MaxAttributesLength = 16_384;
    public const string EmptyAttributes = "{}";

    private Product()
    {
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Money Price { get; private set; } = null!;

    public string Attributes { get; private set; } = EmptyAttributes;

    public static Product Create(ProductDetails details)
    {
        var product = new Product { Id = Guid.CreateVersion7() };
        product.Update(details);
        return product;
    }

    public void Update(ProductDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(details.Price);

        string validType = ValidType(details.Type);
        string validName = ValidName(details.Name);
        string validDescription = ValidDescription(details.Description);
        string validAttributes = ValidAttributes(details.Attributes);

        Type = validType;
        Name = validName;
        Description = validDescription;
        Price = details.Price;
        Attributes = validAttributes;
    }

    private static string ValidType(string type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        string normalized = type.Trim().ToLowerInvariant();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(normalized.Length, MaxTypeLength, nameof(type));

        return normalized;
    }

    private static string ValidName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string trimmed = name.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(trimmed.Length, MaxNameLength, nameof(name));

        return trimmed;
    }

    private static string ValidDescription(string description)
    {
        ArgumentNullException.ThrowIfNull(description);

        string trimmed = description.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(trimmed.Length, MaxDescriptionLength, nameof(description));

        return trimmed;
    }

    private static string ValidAttributes(string attributes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attributes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(attributes.Length, MaxAttributesLength, nameof(attributes));

        try
        {
            using JsonDocument document = JsonDocument.Parse(attributes);

            return document.RootElement.ValueKind == JsonValueKind.Object
                ? attributes
                : throw new ArgumentException("Attributes must be a JSON object.", nameof(attributes));
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Attributes must be valid JSON.", nameof(attributes), exception);
        }
    }
}
