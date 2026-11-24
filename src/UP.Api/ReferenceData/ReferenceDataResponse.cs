namespace UP.Api.ReferenceData;

public sealed record ReferenceDataResponse(
    IReadOnlyList<ProductTypeOption> ProductTypes,
    IReadOnlyList<OrderStatusOption> OrderStatuses);
