namespace UP.Domain.Orders;

public sealed class Order
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public OrderStatus Status { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}
