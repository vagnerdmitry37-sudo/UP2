using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UP.Domain.UserProfiles;
using UP.Infrastructure.Identity;

namespace UP.Infrastructure.UserProfiles;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    private const int CurrencyCodeLength = 3;

    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();

        builder.Property(profile => profile.Currency)
            .HasMaxLength(CurrencyCodeLength)
            .IsFixedLength()
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<UserProfile>(profile => profile.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
