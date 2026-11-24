using UP.Domain.Orders;

namespace UP.Application.Orders;

public sealed record UpdateOrdersResult(
    IReadOnlyList<Order> Updated,
    IReadOnlyList<Guid> MissingOrderIds,
    IReadOnlyList<Guid> MissingCustomerIds);
