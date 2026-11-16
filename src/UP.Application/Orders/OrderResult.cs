using UP.Domain.Orders;

namespace UP.Application.Orders;

public sealed record OrderResult(Order? Order, OrderError? Error = null, string ErrorDetail = "");
