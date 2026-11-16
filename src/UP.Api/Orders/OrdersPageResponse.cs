namespace UP.Api.Orders;

public sealed record OrdersPageResponse(IReadOnlyList<OrderResponse> Items, int TotalRecords);
