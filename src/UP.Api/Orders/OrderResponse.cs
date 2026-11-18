using UP.Domain.Orders;

namespace UP.Api.Orders;

public sealed record OrderResponse(
    Guid Id,
    string Number,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    decimal TotalPrice,
    IReadOnlyList<OrderItemResponse> Items);
