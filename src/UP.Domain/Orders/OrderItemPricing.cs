using UP.Domain.Products;

namespace UP.Domain.Orders;

public static class OrderItemPricing
{
    public static decimal TotalPrice(ProductType type, decimal unitPrice, int passengers) =>
        type is ProductType.GroupExcursion or ProductType.GroupTransfer
            ? unitPrice * passengers
            : unitPrice;
}
