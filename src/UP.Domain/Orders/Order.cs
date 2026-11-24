using UP.Domain.Customers;

namespace UP.Domain.Orders;

public sealed class Order
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public OrderStatus Status { get; set; }

    public Guid? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public decimal TotalPrice { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}
