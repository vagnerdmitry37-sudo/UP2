using System.ComponentModel.DataAnnotations;

namespace UP.Api.Orders;

public sealed record CreateOrderRequest(
    Guid CustomerId,
    [Required, MaxLength(10)] IReadOnlyList<CreateOrderItemRequest> Items);
