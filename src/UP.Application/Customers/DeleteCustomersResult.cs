namespace UP.Application.Customers;

public sealed record DeleteCustomersResult(IReadOnlyList<Guid> MissingIds, IReadOnlyList<Guid> IdsWithOrders);
