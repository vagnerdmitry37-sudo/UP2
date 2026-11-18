using Microsoft.EntityFrameworkCore;

using UP.Application.Customers;
using UP.Domain.Customers;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.Customers;

internal sealed class CustomerService(AppDbContext dbContext, TimeProvider timeProvider) : ICustomerService
{
    private const string LikeEscape = @"\";

    public async Task<Customer?> CreateAsync(Customer customer, CancellationToken cancellationToken)
    {
        bool emailTaken = await dbContext.Customers.AnyAsync(
            existing => existing.Email == customer.Email,
            cancellationToken);

        if (emailTaken)
        {
            return null;
        }

        customer.Id = Guid.CreateVersion7();
        customer.CreatedAt = timeProvider.GetUtcNow();
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return customer;
    }

    public async Task<UpdateCustomersResult> UpdateAsync(
        IReadOnlyList<Customer> customers,
        CancellationToken cancellationToken)
    {
        List<Guid> ids = [.. customers.Select(customer => customer.Id)];
        List<Customer> existingCustomers = await dbContext.Customers
            .Where(customer => ids.Contains(customer.Id))
            .ToListAsync(cancellationToken);
        List<Guid> missingIds = [.. ids.Except(existingCustomers.Select(customer => customer.Id))];

        if (missingIds.Count > 0)
        {
            return new UpdateCustomersResult([], missingIds, []);
        }

        List<string> emails = [.. customers.Select(customer => customer.Email)];
        List<string> takenEmails = await dbContext.Customers
            .Where(customer => emails.Contains(customer.Email) && !ids.Contains(customer.Id))
            .Select(customer => customer.Email)
            .ToListAsync(cancellationToken);

        if (takenEmails.Count > 0)
        {
            return new UpdateCustomersResult([], [], takenEmails);
        }

        foreach (Customer existingCustomer in existingCustomers)
        {
            Customer customer = customers.First(customer => customer.Id == existingCustomer.Id);
            existingCustomer.Name = customer.Name;
            existingCustomer.Email = customer.Email;
            existingCustomer.Phone = customer.Phone;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateCustomersResult(
            [.. ids.Select(id => existingCustomers.First(customer => customer.Id == id))],
            [],
            []);
    }

    public async Task<DeleteCustomersResult> DeleteAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        List<Customer> existingCustomers = await dbContext.Customers
            .Where(customer => ids.Contains(customer.Id))
            .ToListAsync(cancellationToken);
        List<Guid> missingIds = [.. ids.Except(existingCustomers.Select(customer => customer.Id))];

        if (missingIds.Count > 0)
        {
            return new DeleteCustomersResult(missingIds, []);
        }

        List<Guid> idsWithOrders = await dbContext.Orders
            .Where(order => order.CustomerId != null && ids.Contains(order.CustomerId.Value))
            .Select(order => order.CustomerId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (idsWithOrders.Count > 0)
        {
            return new DeleteCustomersResult([], idsWithOrders);
        }

        dbContext.Customers.RemoveRange(existingCustomers);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteCustomersResult([], []);
    }

    public async Task<CustomersPage> GetAllAsync(
        int first,
        int rows,
        string? globalFilter,
        CancellationToken cancellationToken)
    {
        IQueryable<Customer> matching = dbContext.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(globalFilter))
        {
            string pattern = $"%{EscapeLikePattern(globalFilter.Trim())}%";
            matching = matching.Where(customer =>
                EF.Functions.ILike(customer.Name, pattern, LikeEscape)
                || EF.Functions.ILike(customer.Email, pattern, LikeEscape)
                || EF.Functions.ILike(customer.Phone, pattern, LikeEscape));
        }

        int totalRecords = await matching.CountAsync(cancellationToken);
        List<Customer> items = await matching
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

    private static string EscapeLikePattern(string text) =>
        text.Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscape + "_", StringComparison.Ordinal);
}
