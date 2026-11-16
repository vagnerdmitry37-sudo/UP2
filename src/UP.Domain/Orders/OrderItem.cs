using UP.Domain.Products;

namespace UP.Domain.Orders;

public sealed class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public ProductType ProductType { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductAttributes { get; set; } = "{}";

    public decimal UnitPrice { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public int Passengers { get; set; }

    public string PickupAddress { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public decimal TotalPrice { get; set; }
}
