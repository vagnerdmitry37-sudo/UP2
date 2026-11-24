using UP.Domain.Customers;

namespace UP.Application.Customers;

public sealed record CustomersPage(IReadOnlyList<Customer> Items, int TotalRecords);
