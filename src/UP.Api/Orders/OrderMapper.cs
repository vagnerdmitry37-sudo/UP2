using System.Text.Json;

using Riok.Mapperly.Abstractions;

using UP.Application.Orders;
using UP.Domain.Orders;
using UP.Domain.Products;
using UP.Domain.ProductTypes;

namespace UP.Api.Orders;

[Mapper]
internal static partial class OrderMapper
{
    [MapperIgnoreTarget(nameof(Order.Id))]
    [MapperIgnoreTarget(nameof(Order.Number))]
    [MapperIgnoreTarget(nameof(Order.Status))]
    [MapperIgnoreTarget(nameof(Order.CreatedAt))]
    [MapperIgnoreTarget(nameof(Order.TotalPrice))]
    public static partial Order ToOrder(this CreateOrderRequest request);

    [MapperIgnoreTarget(nameof(OrderItem.Id))]
    [MapperIgnoreTarget(nameof(OrderItem.OrderId))]
    [MapperIgnoreTarget(nameof(OrderItem.ProductType))]
    [MapperIgnoreTarget(nameof(OrderItem.ProductName))]
    [MapperIgnoreTarget(nameof(OrderItem.ProductAttributes))]
    [MapperIgnoreTarget(nameof(OrderItem.UnitPrice))]
    [MapperIgnoreTarget(nameof(OrderItem.TotalPrice))]
    public static partial OrderItem ToOrderItem(this CreateOrderItemRequest request);

    [MapperIgnoreTarget(nameof(OrderItem.Id))]
    [MapperIgnoreTarget(nameof(OrderItem.OrderId))]
    [MapperIgnoreTarget(nameof(OrderItem.ProductType))]
    [MapperIgnoreTarget(nameof(OrderItem.ProductName))]
    [MapperIgnoreTarget(nameof(OrderItem.ProductAttributes))]
    [MapperIgnoreTarget(nameof(OrderItem.UnitPrice))]
    [MapperIgnoreTarget(nameof(OrderItem.TotalPrice))]
    public static partial OrderItem ToOrderItem(this UpdateOrderItemRequest request);

    public static partial OrderResponse ToResponse(this Order order);

    public static partial IReadOnlyList<OrderResponse> ToResponses(this IReadOnlyList<Order> orders);

    public static partial OrdersPageResponse ToResponse(this OrdersPage page);

    [MapperIgnoreSource(nameof(OrderItem.OrderId))]
    [MapProperty(nameof(OrderItem.ProductAttributes), nameof(OrderItemResponse.ProductAttributes), Use = nameof(ParseAttributes))]
    [MapProperty(nameof(OrderItem.ProductType), nameof(OrderItemResponse.ProductTypeLabel), Use = nameof(TypeLabelOf))]
    private static partial OrderItemResponse ToItemResponse(OrderItem item);

    private static JsonElement ParseAttributes(string attributes) => JsonSerializer.Deserialize<JsonElement>(attributes);

    private static string TypeLabelOf(ProductType type) => ProductTypeCatalog.For(type).Label;
}
