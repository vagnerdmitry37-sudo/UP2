using UP.Domain.Customers;

namespace UP.Application.Customers;

public interface ICustomerService
{
    Task<Customer?> CreateAsync(Customer customer, CancellationToken cancellationToken);

    Task<UpdateCustomersResult> UpdateAsync(IReadOnlyList<Customer> customers, CancellationToken cancellationToken);

    Task<DeleteCustomersResult> DeleteAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);

    Task<CustomersPage> GetAllAsync(int first, int rows, string? globalFilter, CancellationToken cancellationToken);

    Task<IReadOnlyList<Customer>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);
}
