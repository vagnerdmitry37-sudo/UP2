using UP.Domain.Orders;

namespace UP.Api.Orders;

public sealed record OrderResponse(
    Guid Id,
    string Number,
    OrderStatus Status,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    DateTimeOffset CreatedAt,
    decimal TotalPrice,
    IReadOnlyList<OrderItemResponse> Items);
