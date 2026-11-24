using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using UP.Application.Products;
using UP.Domain.Products;

namespace UP.Api.Products;

[ApiController]
[Route(ProductRoutes.Base)]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    private const int MaxProductsPerRequest = 100;

    [HttpPost]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> Create(
        [MinLength(1), MaxLength(MaxProductsPerRequest)] IReadOnlyList<CreateProductRequest> requests,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Product> created = await productService.CreateAsync(
            [.. requests.Select(request => request.ToProduct())],
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, created.ToResponses());
    }

    [HttpGet]
    [ProducesResponseType<ProductsPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductsPageResponse>> GetAll(
        [FromQuery] GetProductsRequest request,
        CancellationToken cancellationToken)
    {
        ProductsPage page = await productService.GetAllAsync(request.ToQuery(), cancellationToken);

        return Ok(page.ToResponse());
    }

    [HttpGet(ProductRoutes.ByIds)]
    [ProducesResponseType<IReadOnlyList<ProductDetailsResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProductDetailsResponse>>> GetByIds(
        [FromQuery, Required, MinLength(1), MaxLength(MaxProductsPerRequest)] Guid[] ids,
        CancellationToken cancellationToken)
    {
        Guid[] requestedIds = [.. ids.Distinct()];
        IReadOnlyList<ProductDetails> products = await productService.GetByIdsAsync(requestedIds, cancellationToken);

        if (products.Count != requestedIds.Length)
        {
            IEnumerable<Guid> missingIds = requestedIds.Except(products.Select(details => details.Product.Id));
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Products not found: {string.Join(", ", missingIds)}");
        }

        return Ok(products.ToResponses());
    }

    [HttpPut]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> Update(
        [MinLength(1), MaxLength(MaxProductsPerRequest)] IReadOnlyList<UpdateProductRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests.DistinctBy(request => request.Id).Count() != requests.Count)
        {
            ModelState.AddModelError(string.Empty, "Each product can appear only once.");
            return ValidationProblem(ModelState);
        }

        UpdateProductsResult result = await productService.UpdateAsync(
            [.. requests.Select(request => request.ToProduct())],
            cancellationToken);

        if (result.MissingIds.Count > 0)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Products not found: {string.Join(", ", result.MissingIds)}");
        }

        return Ok(result.Updated.ToResponses());
    }

    [HttpDelete]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        [FromQuery, Required, MinLength(1), MaxLength(MaxProductsPerRequest)] Guid[] ids,
        CancellationToken cancellationToken)
    {
        DeleteProductsResult result = await productService.DeleteAsync([.. ids.Distinct()], cancellationToken);

        if (result.MissingIds.Count > 0)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Products not found: {string.Join(", ", result.MissingIds)}");
        }

        if (result.IdsInOrders.Count > 0)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: $"Products are used in orders: {string.Join(", ", result.IdsInOrders)}");
        }

        return NoContent();
    }
}
