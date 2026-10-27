using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using UP.Domain.Images;

namespace UP.Api.Images;

public sealed record CreateImagesRequest(
    [property: FromForm(Name = "entityType"), BindRequired][EnumDataType(typeof(ImageEntityType))] ImageEntityType EntityType,
    [property: FromForm(Name = "entityId"), BindRequired] Guid EntityId,
    [property: FromForm(Name = "files")][Required, MinLength(1), MaxLength(CreateImagesRequest.MaxFiles)] IReadOnlyList<IFormFile> Files)
    : IValidatableObject
{
    public const int MaxFiles = 10;
    public const long MaxFileBytes = 10 * 1024 * 1024;
    public const long MaxRequestBytes = (MaxFiles * MaxFileBytes) + (1024 * 1024);

    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (IFormFile file in Files)
        {
            if (file.Length > MaxFileBytes)
            {
                yield return new ValidationResult(
                    $"File '{file.FileName}' is larger than 10 MB.",
                    [nameof(Files)]);
            }

            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                yield return new ValidationResult(
                    $"File '{file.FileName}' must be a JPEG, PNG or WebP image.",
                    [nameof(Files)]);
            }
        }
    }
}
