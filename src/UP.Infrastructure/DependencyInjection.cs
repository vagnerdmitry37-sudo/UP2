using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using UP.Application.Auth;
using UP.Infrastructure.Auth;
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

        services.AddJwtAuthentication();

        return services;
    }

    private static void AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(options =>
                options.Key.Length >= 32,
                "JWT key must be at least 32 characters long.")
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.Issuer),
                "JWT issuer must be configured.")
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.Audience),
                "JWT audience must be configured.")
            .Validate(options =>
                options.AccessTokenLifetimeMinutes is >= 1 and <= 60,
                "JWT access token lifetime must be between 1 and 60 minutes.")
            .ValidateOnStart();

        services.AddOptions<RefreshTokenOptions>()
            .BindConfiguration(RefreshTokenOptions.SectionName)
            .Validate(options =>
                options.LifetimeDays is >= 1 and <= 30,
                "Refresh token lifetime must be between 1 and 30 days.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                JwtOptions jwt = jwtOptions.Value;

                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = jwt.CreateSigningKey(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = JwtClaimTypes.Role,
                };
            });

        services.AddAuthorization();
    }
}