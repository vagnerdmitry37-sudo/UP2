namespace UP.Api.Customers;

public sealed record CustomersPageResponse(IReadOnlyList<CustomerResponse> Items, int TotalRecords);
