using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UP.Domain.Products;

namespace UP.Infrastructure.Products;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    private const int TypeMaxLength = 50;
    private const int NameMaxLength = 200;
    private const int DescriptionMaxLength = 5000;
    private const int PriceAmountPrecision = 18;
    private const int PriceAmountScale = 2;
    private const int CurrencyCodeLength = 3;

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

        builder.ComplexProperty(product => product.Price, price =>
        {
            price.Property(money => money.Amount)
                .HasColumnName("PriceAmount")
                .HasPrecision(PriceAmountPrecision, PriceAmountScale);

            price.Property(money => money.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(CurrencyCodeLength)
                .IsFixedLength()
                .IsRequired();
        });

        builder.Property(product => product.Attributes)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(product => product.Type);
    }
}
