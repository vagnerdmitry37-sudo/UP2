using UP.Domain.Orders;

namespace UP.Application.Orders;

public sealed record OrderItemsPage(IReadOnlyList<OrderItem> Items, int TotalRecords);
