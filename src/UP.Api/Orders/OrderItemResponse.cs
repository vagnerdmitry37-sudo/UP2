using UP.Domain.Products;

namespace UP.Api.Orders;

public sealed record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    ProductType ProductType,
    string ProductName,
    decimal UnitPrice,
    DateTimeOffset StartsAt,
    int Passengers,
    string PickupAddress,
    string Notes,
    decimal TotalPrice);
