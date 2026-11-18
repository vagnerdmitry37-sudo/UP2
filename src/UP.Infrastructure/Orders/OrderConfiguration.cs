using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UP.Domain.Orders;

namespace UP.Infrastructure.Orders;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public const string NumberSequenceName = "OrderNumbers";

    private const int NumberMaxLength = 20;
    private const int StatusMaxLength = 50;
    private const int CustomerNameMaxLength = 200;
    private const int CustomerEmailMaxLength = 256;
    private const int CustomerPhoneMaxLength = 50;
    private const int PricePrecision = 18;
    private const int PriceScale = 2;

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Metadata.Model.AddSequence(NumberSequenceName);

        builder.ToTable("Orders");

        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();

        builder.Property(order => order.Number)
            .HasMaxLength(NumberMaxLength)
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(StatusMaxLength)
            .IsRequired();

        builder.Property(order => order.CustomerName)
            .HasMaxLength(CustomerNameMaxLength)
            .IsRequired();

        builder.Property(order => order.CustomerEmail)
            .HasMaxLength(CustomerEmailMaxLength)
            .IsRequired();

        builder.Property(order => order.CustomerPhone)
            .HasMaxLength(CustomerPhoneMaxLength)
            .IsRequired();

        builder.Property(order => order.TotalPrice)
            .HasPrecision(PricePrecision, PriceScale);

        builder.HasOne(order => order.Customer)
            .WithMany(customer => customer.Orders)
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(order => order.Number).IsUnique();
        builder.HasIndex(order => order.CreatedAt);
        builder.HasIndex(order => order.Status);
        builder.HasIndex(order => order.CustomerId);
    }
}
