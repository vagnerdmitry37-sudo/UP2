using System.ComponentModel.DataAnnotations;

namespace UP.Api.Orders;

public sealed record CreateOrderRequest(
    [Required, MinLength(1), MaxLength(10)] IReadOnlyList<CreateOrderItemRequest> Items);
