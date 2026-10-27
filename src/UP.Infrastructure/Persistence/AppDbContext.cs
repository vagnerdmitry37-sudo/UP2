using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using UP.Domain.HistoryRecords;
using UP.Domain.Images;
using UP.Domain.Orders;
using UP.Domain.Products;
using UP.Domain.UserProfiles;
using UP.Infrastructure.Auth;
using UP.Infrastructure.Identity;

namespace UP.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<HistoryRecord> HistoryRecords => Set<HistoryRecord>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Image> Images => Set<Image>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
