using System.Text.Json;

using UP.Domain.Products;

namespace UP.Api.Orders;

public sealed record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    ProductType ProductType,
    string ProductTypeLabel,
    string ProductName,
    JsonElement ProductAttributes,
    decimal UnitPrice,
    DateTimeOffset StartsAt,
    int Passengers,
    string PickupAddress,
    string Notes,
    decimal TotalPrice);
