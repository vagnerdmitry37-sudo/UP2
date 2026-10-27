using Amazon.S3;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Respawn;
using Respawn.Graph;

using Testcontainers.PostgreSql;

using UP.Infrastructure.Persistence;

namespace UP.Api.IntegrationTests.TestHost;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtKey = "integration-tests-signing-key-not-a-secret-0123456789";
    public const string ImagesPublicBaseUrl = "https://images.example.test";
    public const string ImagesBucket = "images";

    private const int S3MockPort = 9090;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private readonly IContainer _s3Mock = new ContainerBuilder("adobe/s3mock:5.2.3")
        .WithPortBinding(S3MockPort, true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(request => request.ForPort(S3MockPort).ForPath("/")))
        .Build();

    private Respawner? _respawner;

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _s3Mock.StartAsync());

        await using (AsyncServiceScope scope = Services.CreateAsyncScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        await Services.GetRequiredService<IAmazonS3>().PutBucketAsync(ImagesBucket);

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")],
        });
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null)
        {
            throw new InvalidOperationException("The factory has not been initialized.");
        }

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public HttpClient CreateHttpsClient(bool handleCookies = true) =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = handleCookies,
        });

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        await _s3Mock.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Options:Database:ConnectionString", _postgres.GetConnectionString());
        builder.UseSetting("Options:Jwt:Key", JwtKey);
        builder.UseSetting("Options:R2:Default", $"http://{_s3Mock.Hostname}:{_s3Mock.GetMappedPublicPort(S3MockPort)}");
        builder.UseSetting("Options:R2:AccessKeyID", "test-access-key");
        builder.UseSetting("Options:R2:SecretAccessKey", "test-secret-key");
        builder.UseSetting("Options:R2:Bucket", ImagesBucket);
        builder.UseSetting("Options:R2:PublicBaseUrl", ImagesPublicBaseUrl);
    }
}
