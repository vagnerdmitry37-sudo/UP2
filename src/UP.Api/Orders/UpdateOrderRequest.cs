namespace UP.Api.Orders;

public sealed record UpdateOrderRequest(Guid Id, Guid CustomerId);
