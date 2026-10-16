using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UP.Domain.Common;
using UP.Domain.Products;

namespace UP.Infrastructure.Products;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    private const int PriceAmountPrecision = 18;

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();

        builder.Property(product => product.Type)
            .HasMaxLength(Product.MaxTypeLength)
            .IsRequired();

        builder.Property(product => product.Name)
            .HasMaxLength(Product.MaxNameLength)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasMaxLength(Product.MaxDescriptionLength)
            .IsRequired();

        builder.ComplexProperty(product => product.Price, price =>
        {
            price.Property(money => money.Amount)
                .HasColumnName("PriceAmount")
                .HasPrecision(PriceAmountPrecision, Money.MaxDecimalPlaces);

            price.Property(money => money.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(Money.CurrencyCodeLength)
                .IsFixedLength()
                .IsRequired();
        });

        builder.Property(product => product.Attributes)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(product => product.Type);
    }
}
