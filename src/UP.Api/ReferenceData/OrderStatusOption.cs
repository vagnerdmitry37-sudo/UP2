using UP.Domain.Orders;

namespace UP.Api.ReferenceData;

public sealed record OrderStatusOption(OrderStatus Value, string Label);
