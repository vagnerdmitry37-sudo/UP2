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

    [HttpPost]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponse>> Create(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        Customer? created = await customerService.CreateAsync(request.ToCustomer(), cancellationToken);

        if (created is null)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: $"A customer with email {request.Email} already exists.");
        }

        return StatusCode(StatusCodes.Status201Created, created.ToResponse());
    }

    [HttpPut]
    [ProducesResponseType<IReadOnlyList<CustomerResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> Update(
        [MinLength(1), MaxLength(MaxCustomersPerRequest)] IReadOnlyList<UpdateCustomerRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests.DistinctBy(request => request.Id).Count() != requests.Count)
        {
            ModelState.AddModelError(string.Empty, "Each customer can appear only once.");
            return ValidationProblem(ModelState);
        }

        if (requests.DistinctBy(request => request.Email).Count() != requests.Count)
        {
            ModelState.AddModelError(string.Empty, "Each email can appear only once.");
            return ValidationProblem(ModelState);
        }

        UpdateCustomersResult result = await customerService.UpdateAsync(
            [.. requests.Select(request => request.ToCustomer())],
            cancellationToken);

        if (result.MissingIds.Count > 0)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Customers not found: {string.Join(", ", result.MissingIds)}");
        }

        if (result.TakenEmails.Count > 0)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: $"Emails already used by other customers: {string.Join(", ", result.TakenEmails)}");
        }

        return Ok(result.Updated.ToResponses());
    }

    [HttpGet]
    [ProducesResponseType<CustomersPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomersPageResponse>> GetAll(
        [FromQuery] GetCustomersRequest request,
        CancellationToken cancellationToken)
    {
        CustomersPage page = await customerService.GetAllAsync(
            request.First,
            request.Rows,
            request.GlobalFilter,
            cancellationToken);

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

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        [FromQuery, Required, MinLength(1), MaxLength(MaxCustomersPerRequest)] Guid[] ids,
        CancellationToken cancellationToken)
    {
        DeleteCustomersResult result = await customerService.DeleteAsync([.. ids.Distinct()], cancellationToken);

        if (result.MissingIds.Count > 0)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Customers not found: {string.Join(", ", result.MissingIds)}");
        }

        if (result.IdsWithOrders.Count > 0)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: $"Customers still have orders: {string.Join(", ", result.IdsWithOrders)}");
        }

        return NoContent();
    }
}
