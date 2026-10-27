using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UP.Domain.Images;

namespace UP.Infrastructure.Images;

internal sealed class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    private const int EntityTypeMaxLength = 50;
    private const int KeyMaxLength = 300;

    public void Configure(EntityTypeBuilder<Image> builder)
    {
        builder.ToTable("Images");

        builder.HasKey(image => image.Id);
        builder.Property(image => image.Id).ValueGeneratedNever();

        builder.Property(image => image.EntityType)
            .HasConversion<string>()
            .HasMaxLength(EntityTypeMaxLength)
            .IsRequired();

        builder.Property(image => image.Key)
            .HasMaxLength(KeyMaxLength)
            .IsRequired();

        builder.HasIndex(image => image.Key).IsUnique();
        builder.HasIndex(image => new { image.EntityType, image.EntityId, image.SortOrder });
    }
}
