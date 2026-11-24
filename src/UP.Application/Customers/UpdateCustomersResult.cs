using UP.Domain.Customers;

namespace UP.Application.Customers;

public sealed record UpdateCustomersResult(
    IReadOnlyList<Customer> Updated,
    IReadOnlyList<Guid> MissingIds,
    IReadOnlyList<string> TakenEmails);
