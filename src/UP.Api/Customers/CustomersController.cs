using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using UP.Application.Customers;
using UP.Domain.Customers;

namespace UP.Api.Customers;

[ApiController]
[Route(CustomerRoutes.Base)]
[Authorize]
public sealed class CustomersController(ICustomerService customerService) : ControllerBase
{
    private const int MaxCustomersPerRequest = 100;

    [HttpGet]
    [ProducesResponseType<CustomersPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomersPageResponse>> GetAll(
        [FromQuery] GetCustomersRequest request,
        CancellationToken cancellationToken)
    {
        CustomersPage page = await customerService.GetAllAsync(request.First, request.Rows, cancellationToken);

        return Ok(page.ToResponse());
    }

    [HttpGet(CustomerRoutes.ByIds)]
    [ProducesResponseType<IReadOnlyList<CustomerResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> GetByIds(
        [FromQuery, Required, MinLength(1), MaxLength(MaxCustomersPerRequest)] Guid[] ids,
        CancellationToken cancellationToken)
    {
        Guid[] requestedIds = [.. ids.Distinct()];
        IReadOnlyList<Customer> customers = await customerService.GetByIdsAsync(requestedIds, cancellationToken);

        if (customers.Count != requestedIds.Length)
        {
            IEnumerable<Guid> missingIds = requestedIds.Except(customers.Select(customer => customer.Id));
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Customers not found: {string.Join(", ", missingIds)}");
        }

        return Ok(customers.ToResponses());
    }
}
