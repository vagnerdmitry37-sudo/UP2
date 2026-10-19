using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UP.Domain.Products;

namespace UP.Infrastructure.Products;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    private const int TypeMaxLength = 50;
    private const int NameMaxLength = 200;
    private const int DescriptionMaxLength = 5000;
    private const int PricePrecision = 18;
    private const int PriceScale = 2;

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();

        builder.Property(product => product.Type)
            .HasConversion<string>()
            .HasMaxLength(TypeMaxLength)
            .IsRequired();

        builder.Property(product => product.Name)
            .HasMaxLength(NameMaxLength)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasMaxLength(DescriptionMaxLength)
            .IsRequired();

        builder.Property(product => product.Price)
            .HasPrecision(PricePrecision, PriceScale);

        builder.Property(product => product.Attributes)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(product => product.Type);
    }
}
