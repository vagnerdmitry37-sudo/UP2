using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using UP.Application.Orders;
using UP.Domain.Orders;

namespace UP.Api.Orders;

[ApiController]
[Route(OrderRoutes.Base)]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    private const int MaxOrdersPerRequest = 100;

    private const int MaxOrderItemsPerRequest = 100;

    [HttpPost]
    [Authorize]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Create(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        OrderResult result = await orderService.CreateAsync(request.ToOrder(), cancellationToken);

        return result switch
        {
            { Order: not null } => StatusCode(StatusCodes.Status201Created, result.Order.ToResponse()),
            { Error: OrderError.CustomerNotFound or OrderError.ProductNotFound } => Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: result.ErrorDetail),
            _ => Problem(statusCode: StatusCodes.Status400BadRequest, detail: result.ErrorDetail),
        };
    }

    [HttpPut]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> Update(
        [MinLength(1), MaxLength(MaxOrdersPerRequest)] IReadOnlyList<UpdateOrderRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests.DistinctBy(request => request.Id).Count() != requests.Count)
        {
            ModelState.AddModelError(string.Empty, "Each order can appear only once.");
            return ValidationProblem(ModelState);
        }

        UpdateOrdersResult result = await orderService.UpdateAsync(
            [.. requests.Select(request => request.ToOrder())],
            cancellationToken);

        if (result.MissingOrderIds.Count > 0)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Orders not found: {string.Join(", ", result.MissingOrderIds)}");
        }

        if (result.MissingCustomerIds.Count > 0)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Customers not found: {string.Join(", ", result.MissingCustomerIds)}");
        }

        return Ok(result.Updated.ToResponses());
    }

    [HttpPost(OrderRoutes.Items)]
    [Authorize]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> CreateItem(
        Guid id,
        CreateOrderItemRequest request,
        CancellationToken cancellationToken)
    {
        OrderResult result = await orderService.CreateItemAsync(id, request.ToOrderItem(), cancellationToken);

        return result switch
        {
            { Order: not null } => StatusCode(StatusCodes.Status201Created, result.Order.ToResponse()),
            { Error: OrderError.OrderNotFound or OrderError.ProductNotFound } => Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: result.ErrorDetail),
            _ => Problem(statusCode: StatusCodes.Status400BadRequest, detail: result.ErrorDetail),
        };
    }

    [HttpPut(OrderRoutes.ItemById)]
    [Authorize]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> UpdateItem(
        Guid id,
        Guid itemId,
        UpdateOrderItemRequest request,
        CancellationToken cancellationToken)
    {
        OrderResult result = await orderService.UpdateItemAsync(
            id,
            itemId,
            request.ToOrderItem(),
            cancellationToken);

        return result switch
        {
            { Order: not null } => Ok(result.Order.ToResponse()),
            { Error: OrderError.OrderNotFound or OrderError.ItemNotFound or OrderError.ProductNotFound } => Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: result.ErrorDetail),
            _ => Problem(statusCode: StatusCodes.Status400BadRequest, detail: result.ErrorDetail),
        };
    }

    [HttpDelete(OrderRoutes.Items)]
    [Authorize]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> DeleteItems(
        Guid id,
        [FromQuery, Required, MinLength(1), MaxLength(MaxOrderItemsPerRequest)] Guid[] ids,
        CancellationToken cancellationToken)
    {
        OrderResult result = await orderService.DeleteItemsAsync(id, [.. ids.Distinct()], cancellationToken);

        if (result.Order is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, detail: result.ErrorDetail);
        }

        return Ok(result.Order.ToResponse());
    }

    [HttpGet(OrderRoutes.ById)]
    [Authorize]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        Order? order = await orderService.GetAsync(id, cancellationToken);

        if (order is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Order {id} not found.");
        }

        return Ok(order.ToResponse());
    }

    [HttpGet(OrderRoutes.ByIds)]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetByIds(
        [FromQuery, Required, MinLength(1), MaxLength(MaxOrdersPerRequest)] Guid[] ids,
        CancellationToken cancellationToken)
    {
        Guid[] requestedIds = [.. ids.Distinct()];
        IReadOnlyList<Order> orders = await orderService.GetByIdsAsync(requestedIds, cancellationToken);

        if (orders.Count != requestedIds.Length)
        {
            IEnumerable<Guid> missingIds = requestedIds.Except(orders.Select(order => order.Id));
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Orders not found: {string.Join(", ", missingIds)}");
        }

        return Ok(orders.ToResponses());
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType<OrdersPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OrdersPageResponse>> GetAll(
        [FromQuery] GetOrdersRequest request,
        CancellationToken cancellationToken)
    {
        OrdersPage page = await orderService.GetAllAsync(request.First, request.Rows, cancellationToken);

        return Ok(page.ToResponse());
    }

    [HttpDelete]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromQuery, Required, MinLength(1), MaxLength(MaxOrdersPerRequest)] Guid[] ids,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> missingIds = await orderService.DeleteAsync([.. ids.Distinct()], cancellationToken);

        if (missingIds.Count > 0)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Orders not found: {string.Join(", ", missingIds)}");
        }

        return NoContent();
    }
}
