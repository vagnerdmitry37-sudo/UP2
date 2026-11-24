using UP.Domain.Orders;

namespace UP.Application.Orders;

public sealed record OrderItemResult(OrderItem? Item, OrderError? Error = null, string ErrorDetail = "");
