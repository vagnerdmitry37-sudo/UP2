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

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<Product> products = await productService.GetAllAsync(cancellationToken);

        return Ok(products.ToResponses());
    }

    [HttpGet(ProductRoutes.ById)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        Product? product = await productService.GetAsync(id, cancellationToken);

        return product is null ? NotFound() : Ok(product.ToResponse());
    }

    [HttpPut(ProductRoutes.ById)]
    [Authorize]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Update(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        Product product = request.ToProduct();
        product.Id = id;

        Product? updated = await productService.UpdateAsync(product, cancellationToken);

        return updated is null ? NotFound() : Ok(updated.ToResponse());
    }
}
