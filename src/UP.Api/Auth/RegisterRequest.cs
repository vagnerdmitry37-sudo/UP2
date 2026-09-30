using System.ComponentModel.DataAnnotations;

namespace UP.Api.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(128)] string Password);
