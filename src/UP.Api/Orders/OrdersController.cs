using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using UP.Application.Orders;
using UP.Domain.Orders;

namespace UP.Api.Orders;

[ApiController]
[Route(OrderRoutes.Base)]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Create(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        OrderResult result = await orderService.CreateAsync(request.ToOrder(), cancellationToken);

        return result switch
        {
            { Order: not null } => StatusCode(StatusCodes.Status201Created, result.Order.ToResponse()),
            { Error: OrderError.ProductNotFound } => Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: result.ErrorDetail),
            _ => Problem(statusCode: StatusCodes.Status400BadRequest, detail: result.ErrorDetail),
        };
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

    [HttpPut(OrderRoutes.Customer)]
    [Authorize]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> UpdateCustomer(
        Guid id,
        UpdateOrderCustomerRequest request,
        CancellationToken cancellationToken)
    {
        Order? order = await orderService.UpdateCustomerAsync(
            id,
            request.CustomerName,
            request.CustomerEmail,
            request.CustomerPhone,
            cancellationToken);

        if (order is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Order {id} not found.");
        }

        return Ok(order.ToResponse());
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
}
