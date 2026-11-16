using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using UP.Application.Orders;

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
        CreateOrderResult result = await orderService.CreateAsync(request.ToOrder(), cancellationToken);

        return result switch
        {
            { Created: not null } => StatusCode(StatusCodes.Status201Created, result.Created.ToResponse()),
            { Error: CreateOrderError.ProductNotFound } => Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: result.ErrorDetail),
            _ => Problem(statusCode: StatusCodes.Status400BadRequest, detail: result.ErrorDetail),
        };
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
