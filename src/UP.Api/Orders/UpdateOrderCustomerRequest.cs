using System.ComponentModel.DataAnnotations;

namespace UP.Api.Orders;

public sealed record UpdateOrderCustomerRequest(
    [Required, MaxLength(200)] string CustomerName,
    [Required, EmailAddress, MaxLength(256)] string CustomerEmail,
    [Required, MaxLength(50)] string CustomerPhone);
