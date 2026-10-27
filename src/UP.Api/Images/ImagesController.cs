using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using UP.Application.Images;
using UP.Domain.Images;

namespace UP.Api.Images;

[ApiController]
[Route(ImageRoutes.Base)]
public sealed class ImagesController(IImageService imageService) : ControllerBase
{
    private const int MaxEntitiesPerRequest = 100;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ImageResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ImageResponse>>> GetAll(
        [FromQuery, BindRequired, EnumDataType(typeof(ImageEntityType))] ImageEntityType entityType,
        [FromQuery, Required, MinLength(1), MaxLength(MaxEntitiesPerRequest)] Guid[] entityIds,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PublicImage> images = await imageService.GetAllAsync(
            entityType,
            [.. entityIds.Distinct()],
            cancellationToken);

        return Ok(images.ToResponses());
    }
}
