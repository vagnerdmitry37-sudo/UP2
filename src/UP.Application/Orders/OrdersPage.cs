using UP.Domain.Orders;

namespace UP.Application.Orders;

public sealed record OrdersPage(IReadOnlyList<Order> Items, int TotalRecords);
