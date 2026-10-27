using System.ComponentModel.DataAnnotations;

using UP.Domain.Images;

namespace UP.Api.Images;

public sealed record UpdateImageOrderRequest(
    [EnumDataType(typeof(ImageEntityType))] ImageEntityType EntityType,
    Guid EntityId,
    [Required, MinLength(1), MaxLength(UpdateImageOrderRequest.MaxImages)] IReadOnlyList<Guid> ImageIds)
    : IValidatableObject
{
    public const int MaxImages = 100;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ImageIds.Distinct().Count() != ImageIds.Count)
        {
            yield return new ValidationResult("Each image can appear only once.", [nameof(ImageIds)]);
        }
    }
}
