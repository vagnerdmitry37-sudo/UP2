using UP.Domain.Customers;

namespace UP.Application.Customers;

public interface ICustomerService
{
    Task<Customer?> CreateAsync(Customer customer, CancellationToken cancellationToken);

    Task<CustomersPage> GetAllAsync(int first, int rows, CancellationToken cancellationToken);

    Task<IReadOnlyList<Customer>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);
}
