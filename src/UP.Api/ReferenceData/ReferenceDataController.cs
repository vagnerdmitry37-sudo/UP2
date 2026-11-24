using Microsoft.AspNetCore.Mvc;

using UP.Domain.Orders;
using UP.Domain.Products;

namespace UP.Api.ReferenceData;

[ApiController]
[Route(ReferenceDataRoutes.Base)]
public sealed class ReferenceDataController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ReferenceDataResponse>(StatusCodes.Status200OK)]
    public ActionResult<ReferenceDataResponse> Get() => Ok(new ReferenceDataResponse(
        [.. Enum.GetValues<ProductType>().Select(type => new ProductTypeOption(type, type.Label()))],
        [.. Enum.GetValues<OrderStatus>().Select(status => new OrderStatusOption(status, status.Label()))]));
}
