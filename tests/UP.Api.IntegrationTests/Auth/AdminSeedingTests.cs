using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

using UP.Api.Auth;
using UP.Api.IntegrationTests.TestHost;
using UP.Application.Auth;

namespace UP.Api.IntegrationTests.Auth;

public sealed class AdminSeedingTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string AdminEmail = "admin@example.com";
    private const string AdminPassword = "admin-password-0123";
    private const string RoleClaim = "role";

    [Fact]
    public async Task Configured_admin_can_log_in_with_admin_role()
    {
        using WebApplicationFactory<Program> host = HostWithAdmin(AdminPassword);
        using HttpClient client = CreateHttpsClient(host);

        HttpResponseMessage response = await client.LoginAsync(AdminEmail, AdminPassword);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AccessTokenResponse token = await response.ReadAccessTokenAsync();
        new JsonWebToken(token.AccessToken)
            .Claims.Where(claim => claim.Type == RoleClaim)
            .Select(claim => claim.Value)
            .ShouldContain(Roles.Admin);
    }

    [Fact]
    public async Task Restarting_with_same_admin_configuration_keeps_single_admin()
    {
        using (WebApplicationFactory<Program> firstStart = HostWithAdmin(AdminPassword))
        {
            CreateHttpsClient(firstStart).Dispose();
        }

        using WebApplicationFactory<Program> secondStart = HostWithAdmin(AdminPassword);
        using HttpClient client = CreateHttpsClient(secondStart);

        HttpResponseMessage response = await client.LoginAsync(AdminEmail, AdminPassword);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        int admins = await QueryDatabaseAsync(db => db.Users.CountAsync(user => user.Email == AdminEmail, CancellationToken));
        admins.ShouldBe(1);
    }

    [Fact]
    public async Task Changed_configured_password_does_not_reset_existing_admin()
    {
        using (WebApplicationFactory<Program> firstStart = HostWithAdmin(AdminPassword))
        {
            CreateHttpsClient(firstStart).Dispose();
        }

        const string newPassword = "another-password-4567";
        using WebApplicationFactory<Program> secondStart = HostWithAdmin(newPassword);
        using HttpClient client = CreateHttpsClient(secondStart);

        (await client.LoginAsync(AdminEmail, AdminPassword)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.LoginAsync(AdminEmail, newPassword)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_is_not_created_without_configuration()
    {
        HttpResponseMessage response = await Client.LoginAsync(AdminEmail, AdminPassword);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private WebApplicationFactory<Program> HostWithAdmin(string password) =>
        Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Options:Admin:Email", AdminEmail);
            builder.UseSetting("Options:Admin:Password", password);
        });

    private static HttpClient CreateHttpsClient(WebApplicationFactory<Program> host) =>
        host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
}
