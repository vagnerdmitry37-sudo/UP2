using Microsoft.AspNetCore.Mvc;

using UP.Domain.ProductTypes;

namespace UP.Api.ProductTypes;

[ApiController]
[Route(ProductTypeRoutes.Base)]
public sealed class ProductTypesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductTypeResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ProductTypeResponse>> GetAll() => Ok(ProductTypeCatalog.All.ToResponses());
}
