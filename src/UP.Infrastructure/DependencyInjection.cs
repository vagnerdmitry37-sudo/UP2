using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using UP.Infrastructure.Identity;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Database connection string must be configured.")
            .Validate(options =>
                options.MaxRetryCount is >= 0 and <= 10,
                "Database max retry count must be between 0 and 10.")
            .Validate(options =>
                options.CommandTimeoutSeconds is >= 1 and <= 600,
                "Database command timeout must be between 1 and 600 seconds.")
            .ValidateOnStart();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            DatabaseOptions database = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            options.UseNpgsql(database.ConnectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(database.MaxRetryCount);
                npgsql.CommandTimeout(database.CommandTimeoutSeconds);
            });

            options.EnableSensitiveDataLogging(database.EnableSensitiveDataLogging);
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        return services;
    }
}