using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;
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
    private const int MaxImagesPerRequest = 100;

    [HttpPost]
    [Authorize]
    [RequestSizeLimit(CreateImagesRequest.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = CreateImagesRequest.MaxRequestBytes)]
    [ProducesResponseType<IReadOnlyList<ImageResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ImageResponse>>> Create(
        [FromForm] CreateImagesRequest request,
        CancellationToken cancellationToken)
    {
        CreateImagesResult result = await imageService.CreateAsync(
            request.EntityType,
            request.EntityId,
            [.. request.Files.Select(file => file.OpenReadStream())],
            cancellationToken);

        if (!result.EntityExists)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"{request.EntityType} not found: {request.EntityId}");
        }

        foreach (int index in result.InvalidFileIndexes)
        {
            ModelState.AddModelError(
                nameof(request.Files),
                $"File '{request.Files[index].FileName}' is not a valid JPEG, PNG or WebP image.");
        }

        return ModelState.IsValid
            ? StatusCode(StatusCodes.Status201Created, result.Created.ToResponses())
            : ValidationProblem(ModelState);
    }

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

    [HttpDelete]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromQuery, Required, MinLength(1), MaxLength(MaxImagesPerRequest)] Guid[] ids,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> missingIds = await imageService.DeleteAsync([.. ids.Distinct()], cancellationToken);

        if (missingIds.Count > 0)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Images not found: {string.Join(", ", missingIds)}");
        }

        return NoContent();
    }
}
