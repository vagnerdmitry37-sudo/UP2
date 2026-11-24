namespace UP.Api.Customers;

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    DateTimeOffset CreatedAt);
