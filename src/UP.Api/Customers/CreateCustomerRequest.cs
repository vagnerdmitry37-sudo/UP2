using System.ComponentModel.DataAnnotations;

namespace UP.Api.Customers;

public sealed record CreateCustomerRequest(
    [Required, MaxLength(200)] string Name,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(50)] string Phone);
