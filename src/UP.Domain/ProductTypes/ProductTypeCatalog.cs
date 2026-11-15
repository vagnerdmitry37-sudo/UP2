using UP.Domain.Products;

namespace UP.Domain.ProductTypes;

public static class ProductTypeCatalog
{
    private const int LocationMaxLength = 200;

    private static readonly AttributeDefinition DurationMinutes =
        new("durationMinutes", "Duration, minutes", AttributeKind.WholeNumber, Required: true, Min: 1);

    private static readonly AttributeDefinition MaxPassengers =
        new("maxPassengers", "Max passengers", AttributeKind.WholeNumber, Required: true, Min: 1);

    private static readonly AttributeDefinition From =
        new("from", "Pickup location", AttributeKind.Text, Required: true, MaxLength: LocationMaxLength);

    private static readonly AttributeDefinition To =
        new("to", "Drop-off location", AttributeKind.Text, Required: true, MaxLength: LocationMaxLength);

    public static IReadOnlyList<ProductTypeDefinition> All { get; } =
    [
        new(
            ProductType.GroupExcursion,
            "Group excursion",
            "Price per person",
            [
                DurationMinutes,
                new("meetingPoint", "Meeting point", AttributeKind.Text, Required: true, MaxLength: LocationMaxLength),
            ]),
        new(ProductType.IndividualExcursion, "Individual excursion", "Price per group", [DurationMinutes, MaxPassengers]),
        new(ProductType.GroupTransfer, "Group transfer", "Price per person", [From, To]),
        new(ProductType.IndividualTransfer, "Individual transfer", "Price per vehicle", [From, To, MaxPassengers]),
    ];

    public static ProductTypeDefinition For(ProductType type) =>
        All.Single(definition => definition.Type == type);
}
