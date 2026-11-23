using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UP.Domain.Orders;
using UP.Domain.Products;

namespace UP.Infrastructure.Orders;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    private const int ProductTypeMaxLength = 50;
    private const int ProductNameMaxLength = 200;
    private const int PickupAddressMaxLength = 300;
    private const int NotesMaxLength = 1000;
    private const int PricePrecision = 18;
    private const int PriceScale = 2;

    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.Property(item => item.ProductType)
            .HasConversion<string>()
            .HasMaxLength(ProductTypeMaxLength)
            .IsRequired();

        builder.Property(item => item.ProductName)
            .HasMaxLength(ProductNameMaxLength)
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasPrecision(PricePrecision, PriceScale);

        builder.Property(item => item.PickupAddress)
            .HasMaxLength(PickupAddressMaxLength)
            .IsRequired();

        builder.Property(item => item.Notes)
            .HasMaxLength(NotesMaxLength)
            .IsRequired();

        builder.Property(item => item.TotalPrice)
            .HasPrecision(PricePrecision, PriceScale);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.ProductId);
        builder.HasIndex(item => item.StartsAt);
    }
}
