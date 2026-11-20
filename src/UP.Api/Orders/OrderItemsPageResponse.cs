namespace UP.Api.Orders;

public sealed record OrderItemsPageResponse(IReadOnlyList<OrderItemResponse> Items, int TotalRecords);
