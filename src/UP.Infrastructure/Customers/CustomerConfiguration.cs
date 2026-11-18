using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UP.Domain.Customers;

namespace UP.Infrastructure.Customers;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    private const int NameMaxLength = 200;
    private const int EmailMaxLength = 256;
    private const int PhoneMaxLength = 50;

    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id).ValueGeneratedNever();

        builder.Property(customer => customer.Name)
            .HasMaxLength(NameMaxLength)
            .IsRequired();

        builder.Property(customer => customer.Email)
            .HasMaxLength(EmailMaxLength)
            .IsRequired();

        builder.Property(customer => customer.Phone)
            .HasMaxLength(PhoneMaxLength)
            .IsRequired();

        builder.HasIndex(customer => customer.Email).IsUnique();
    }
}
