using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using UP.Domain.Products;
using UP.Domain.ProductTypes;

namespace UP.Api.Products;

internal static class ProductAttributesValidator
{
    private static readonly string[] AttributesMember = [nameof(CreateProductRequest.Attributes)];

    public static IEnumerable<ValidationResult> Validate(ProductType type, JsonElement attributes)
    {
        if (!Enum.IsDefined(type))
        {
            yield break;
        }

        if (attributes.ValueKind != JsonValueKind.Object)
        {
            yield return new ValidationResult("Attributes must be an object.", AttributesMember);
            yield break;
        }

        ProductTypeDefinition definition = ProductTypeCatalog.For(type);
        HashSet<string> knownKeys = [.. definition.Attributes.Select(attribute => attribute.Key)];

        foreach (JsonProperty property in attributes.EnumerateObject())
        {
            if (!knownKeys.Contains(property.Name))
            {
                yield return new ValidationResult($"{type} has no attribute {property.Name}.", AttributesMember);
            }
        }

        foreach (AttributeDefinition attribute in definition.Attributes)
        {
            string? error = Check(attribute, attributes);
            if (error is not null)
            {
                yield return new ValidationResult(error, AttributesMember);
            }
        }
    }

    private static string? Check(AttributeDefinition attribute, JsonElement attributes)
    {
        if (!attributes.TryGetProperty(attribute.Key, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return attribute.Required ? $"Enter {attribute.Key}." : null;
        }

        return attribute.Kind switch
        {
            AttributeKind.Text => CheckText(attribute, value),
            AttributeKind.WholeNumber => CheckWholeNumber(attribute, value),
            _ => $"{attribute.Key} has an unsupported kind.",
        };
    }

    private static string? CheckText(AttributeDefinition attribute, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            return $"{attribute.Key} must be text.";
        }

        string text = value.GetString() ?? "";
        if (attribute.Required && string.IsNullOrWhiteSpace(text))
        {
            return $"Enter {attribute.Key}.";
        }

        return text.Length > attribute.MaxLength
            ? $"Use {attribute.MaxLength} characters or fewer for {attribute.Key}."
            : null;
    }

    private static string? CheckWholeNumber(AttributeDefinition attribute, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int number))
        {
            return $"{attribute.Key} must be a whole number.";
        }

        return number < attribute.Min ? $"Enter {attribute.Min} or more for {attribute.Key}." : null;
    }
}
