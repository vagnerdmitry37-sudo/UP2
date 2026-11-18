using Microsoft.EntityFrameworkCore;

using UP.Application.Customers;
using UP.Domain.Customers;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.Customers;

internal sealed class CustomerService(AppDbContext dbContext) : ICustomerService
{
    public async Task<CustomersPage> GetAllAsync(int first, int rows, CancellationToken cancellationToken)
    {
        int totalRecords = await dbContext.Customers.CountAsync(cancellationToken);
        List<Customer> items = await dbContext.Customers
            .AsNoTracking()
            .OrderByDescending(customer => customer.CreatedAt)
            .ThenByDescending(customer => customer.Id)
            .Skip(first)
            .Take(rows)
            .ToListAsync(cancellationToken);

        return new CustomersPage(items, totalRecords);
    }

    public async Task<IReadOnlyList<Customer>> GetByIdsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        List<Customer> customers = await dbContext.Customers
            .AsNoTracking()
            .Where(customer => ids.Contains(customer.Id))
            .ToListAsync(cancellationToken);

        return [.. ids.Select(id => customers.Find(customer => customer.Id == id)).OfType<Customer>()];
    }
}
