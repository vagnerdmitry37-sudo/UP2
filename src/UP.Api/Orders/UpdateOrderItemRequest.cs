using System.ComponentModel.DataAnnotations;

namespace UP.Api.Orders;

public sealed record UpdateOrderItemRequest(
    Guid ProductId,
    DateTimeOffset StartsAt,
    [Range(1, 100)] int Passengers,
    [Required, MaxLength(300)] string PickupAddress,
    [Required(AllowEmptyStrings = true), MaxLength(1000)] string Notes = "");
