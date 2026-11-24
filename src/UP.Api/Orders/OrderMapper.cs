using Riok.Mapperly.Abstractions;

using UP.Api.Customers;
using UP.Application.Orders;
using UP.Domain.Orders;
using UP.Domain.Products;

namespace UP.Api.Orders;

[Mapper]
[UseStaticMapper(typeof(CustomerMapper))]
internal static partial class OrderMapper
{
    [MapperIgnoreTarget(nameof(Order.Id))]
    [MapperIgnoreTarget(nameof(Order.Number))]
    [MapperIgnoreTarget(nameof(Order.Status))]
    [MapperIgnoreTarget(nameof(Order.Customer))]
    [MapperIgnoreTarget(nameof(Order.CreatedAt))]
    [MapperIgnoreTarget(nameof(Order.TotalPrice))]
    public static partial Order ToOrder(this CreateOrderRequest request);

    [MapperIgnoreTarget(nameof(Order.Number))]
    [MapperIgnoreTarget(nameof(Order.Status))]
    [MapperIgnoreTarget(nameof(Order.Customer))]
    [MapperIgnoreTarget(nameof(Order.CreatedAt))]
    [MapperIgnoreTarget(nameof(Order.TotalPrice))]
    [MapperIgnoreTarget(nameof(Order.Items))]
    public static partial Order ToOrder(this UpdateOrderRequest request);

    [MapperIgnoreTarget(nameof(OrderItem.Id))]
    [MapperIgnoreTarget(nameof(OrderItem.OrderId))]
    [MapperIgnoreTarget(nameof(OrderItem.ProductType))]
    [MapperIgnoreTarget(nameof(OrderItem.ProductName))]
    [MapperIgnoreTarget(nameof(OrderItem.UnitPrice))]
    [MapperIgnoreTarget(nameof(OrderItem.TotalPrice))]
    public static partial OrderItem ToOrderItem(this CreateOrderItemRequest request);

    [MapperIgnoreTarget(nameof(OrderItem.OrderId))]
    [MapperIgnoreTarget(nameof(OrderItem.ProductType))]
    [MapperIgnoreTarget(nameof(OrderItem.ProductName))]
    [MapperIgnoreTarget(nameof(OrderItem.UnitPrice))]
    [MapperIgnoreTarget(nameof(OrderItem.TotalPrice))]
    public static partial OrderItem ToOrderItem(this UpdateOrderItemRequest request);

    [MapperIgnoreSource(nameof(Order.CustomerId))]
    [MapProperty(nameof(Order.Items), nameof(OrderResponse.Items), Use = nameof(ToResponsesByStartTime))]
    public static partial OrderResponse ToResponse(this Order order);

    public static partial IReadOnlyList<OrderResponse> ToResponses(this IReadOnlyList<Order> orders);

    public static partial OrdersPageResponse ToResponse(this OrdersPage page);

    [MapperIgnoreSource(nameof(OrderItem.OrderId))]
    public static partial OrderItemResponse ToResponse(this OrderItem item);

    private static IReadOnlyList<OrderItemResponse> ToResponsesByStartTime(List<OrderItem> items) =>
        [.. items.OrderBy(item => item.StartsAt).ThenBy(item => item.Id).Select(item => item.ToResponse())];
}
