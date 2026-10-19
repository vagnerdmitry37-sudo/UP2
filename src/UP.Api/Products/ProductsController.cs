using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using UP.Application.Products;
using UP.Domain.Products;

namespace UP.Api.Products;

[ApiController]
[Route(ProductRoutes.Base)]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpPost]
    [Authorize]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProductResponse>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        Product created = await productService.CreateAsync(request.ToProduct(), cancellationToken);

        return Created($"/{ProductRoutes.Base}/{created.Id}", created.ToResponse());
    }
}
