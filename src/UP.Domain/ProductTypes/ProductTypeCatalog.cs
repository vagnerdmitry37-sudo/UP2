using UP.Domain.Products;

namespace UP.Domain.ProductTypes;

public static class ProductTypeCatalog
{
    private const int LocationMaxLength = 200;

    public static IReadOnlyList<ProductTypeDefinition> All { get; } =
    [
        new(
            ProductType.Excursion,
            "Price",
            [
                new("durationMinutes", "Duration, minutes", AttributeKind.WholeNumber, Required: false, Min: 1),
                new("meetingPoint", "Meeting point", AttributeKind.Text, Required: false, MaxLength: LocationMaxLength),
            ]),
        new(
            ProductType.Transfer,
            "Price per vehicle",
            [
                new("from", "Pickup location", AttributeKind.Text, Required: true, MaxLength: LocationMaxLength),
                new("to", "Drop-off location", AttributeKind.Text, Required: true, MaxLength: LocationMaxLength),
                new("maxPassengers", "Max passengers", AttributeKind.WholeNumber, Required: true, Min: 1),
            ]),
    ];

    public static ProductTypeDefinition For(ProductType type) =>
        All.Single(definition => definition.Type == type);
}
