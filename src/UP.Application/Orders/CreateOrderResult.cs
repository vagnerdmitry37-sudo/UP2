using UP.Domain.Orders;

namespace UP.Application.Orders;

public sealed record CreateOrderResult(Order? Created, CreateOrderError? Error = null, string ErrorDetail = "");
