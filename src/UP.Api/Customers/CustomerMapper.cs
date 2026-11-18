using Riok.Mapperly.Abstractions;

using UP.Application.Customers;
using UP.Domain.Customers;

namespace UP.Api.Customers;

[Mapper]
internal static partial class CustomerMapper
{
    [MapperIgnoreTarget(nameof(Customer.Id))]
    [MapperIgnoreTarget(nameof(Customer.CreatedAt))]
    [MapperIgnoreTarget(nameof(Customer.Orders))]
    public static partial Customer ToCustomer(this CreateCustomerRequest request);

    [MapperIgnoreSource(nameof(Customer.Orders))]
    public static partial CustomerResponse ToResponse(this Customer customer);

    public static partial IReadOnlyList<CustomerResponse> ToResponses(this IReadOnlyList<Customer> customers);

    public static partial CustomersPageResponse ToResponse(this CustomersPage page);
}
