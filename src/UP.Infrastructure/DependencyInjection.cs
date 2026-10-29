using Amazon.Runtime;
using Amazon.S3;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using UP.Application.Auth;
using UP.Application.HistoryRecords;
using UP.Application.Images;
using UP.Application.Products;
using UP.Application.UserProfiles;
using UP.Infrastructure.Auth;
using UP.Infrastructure.HistoryRecords;
using UP.Infrastructure.Identity;
using UP.Infrastructure.Images;
using UP.Infrastructure.Persistence;
using UP.Infrastructure.Products;
using UP.Infrastructure.UserProfiles;

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
            options.AddInterceptors(serviceProvider.GetRequiredService<HistoryInterceptor>());
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
        services.AddAdminSeeding();
        services.AddR2Storage();

        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IHistoryRecordService, HistoryRecordService>();
        services.AddScoped<HistoryInterceptor>();

        return services;
    }

    private static void AddAdminSeeding(this IServiceCollection services)
    {
        services.AddOptions<AdminOptions>()
            .BindConfiguration(AdminOptions.SectionName)
            .Validate(options =>
                !options.IsConfigured || options.Email.Contains('@', StringComparison.Ordinal),
                "Admin email must be a valid email address.")
            .Validate(options =>
                !options.IsConfigured || options.Password.Length >= AdminOptions.MinimumPasswordLength,
                $"Admin password must be at least {AdminOptions.MinimumPasswordLength} characters long.")
            .ValidateOnStart();

        services.AddHostedService<AdminSeeder>();
    }

    private static void AddR2Storage(this IServiceCollection services)
    {
        services.AddOptions<R2Options>()
            .BindConfiguration(R2Options.SectionName)
            .Validate(options =>
                Uri.TryCreate(options.ServiceUrl, UriKind.Absolute, out _),
                "R2 service URL must be an absolute URL.")
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.AccessKeyId) && !string.IsNullOrWhiteSpace(options.SecretAccessKey),
                "R2 access key ID and secret access key must be configured.")
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.Bucket),
                "R2 bucket must be configured.")
            .Validate(options =>
                Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out _),
                "R2 public base URL must be an absolute URL.")
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            R2Options r2 = serviceProvider.GetRequiredService<IOptions<R2Options>>().Value;

            return new AmazonS3Client(
                new BasicAWSCredentials(r2.AccessKeyId, r2.SecretAccessKey),
                new AmazonS3Config
                {
                    ServiceURL = r2.ServiceUrl,
                    AuthenticationRegion = "auto",
                    ForcePathStyle = true,
                    RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                    ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
                });
        });
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
                options.AccessTokenLifetimeSeconds is >= 1 and <= 3600,
                "JWT access token lifetime must be between 1 and 3600 seconds.")
            .ValidateOnStart();

        services.AddOptions<RefreshTokenOptions>()
            .BindConfiguration(RefreshTokenOptions.SectionName)
            .Validate(options =>
                options.LifetimeDays is >= 1 and <= 30,
                "Refresh token lifetime must be between 1 and 30 days.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddSingleton<RefreshTokenFactory>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAuthService, AuthService>();

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
