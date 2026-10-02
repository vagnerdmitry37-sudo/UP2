using System.Net;

using Microsoft.EntityFrameworkCore;

using UP.Api.IntegrationTests.TestHost;

namespace UP.Api.IntegrationTests.Auth;

public sealed class LogoutAllTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "alice@example.com";
    private const string Password = "correct-horse-battery";
    private const int RaceAttempts = 5;

    [Fact]
    public async Task Logout_all_ends_every_session_of_user()
    {
        HttpClient phone = CreateSessionClient();
        await Client.RegisterAndLoginAsync(Email, Password);
        await phone.LoginAsync(Email, Password);

        HttpResponseMessage logoutAll = await Client.LogoutAllAsync();

        logoutAll.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await phone.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_all_keeps_other_users_sessions_alive()
    {
        HttpClient bob = CreateSessionClient();
        await Client.RegisterAndLoginAsync(Email, Password);
        await bob.RegisterAndLoginAsync("bob@example.com", Password);

        await Client.LogoutAllAsync();

        (await bob.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_all_deletes_refresh_cookie()
    {
        await Client.RegisterAndLoginAsync(Email, Password);

        HttpResponseMessage logoutAll = await Client.LogoutAllAsync();

        RefreshCookie.IsDeletedBy(logoutAll).ShouldBeTrue();
    }

    [Fact]
    public async Task User_can_log_in_again_after_logout_all()
    {
        await Client.RegisterAndLoginAsync(Email, Password);
        await Client.LogoutAllAsync();

        HttpResponseMessage login = await Client.LoginAsync(Email, Password);

        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_all_without_session_is_rejected()
    {
        HttpResponseMessage logoutAll = await Client.LogoutAllAsync();

        logoutAll.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_all_with_unknown_token_is_rejected()
    {
        HttpResponseMessage logoutAll = await CreateClientWithoutCookies()
            .PostWithRefreshTokenAsync(AuthApi.LogoutAllRoute, "not-a-real-refresh-token");

        logoutAll.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_all_with_outdated_token_is_rejected_and_ends_only_that_session()
    {
        HttpClient phone = CreateSessionClient();
        await Client.RegisterAsync(Email, Password);
        string outdatedToken = RefreshCookie.ValueFrom(await Client.LoginAsync(Email, Password));
        await phone.LoginAsync(Email, Password);
        await Client.RefreshAsync();

        HttpResponseMessage logoutAll = await CreateClientWithoutCookies()
            .PostWithRefreshTokenAsync(AuthApi.LogoutAllRoute, outdatedToken);

        logoutAll.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await Client.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await phone.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_all_with_expired_token_is_rejected_and_ends_no_session()
    {
        HttpClient phone = CreateSessionClient();
        await Client.RegisterAndLoginAsync(Email, Password);
        await phone.LoginAsync(Email, Password);
        await ExpireOldestRefreshTokenAsync();

        HttpResponseMessage logoutAll = await Client.LogoutAllAsync();

        logoutAll.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await phone.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Parallel_logout_all_and_refresh_leave_no_usable_session()
    {
        await Client.RegisterAsync(Email, Password);
        HttpClient loggingOut = CreateClientWithoutCookies();
        HttpClient refreshing = CreateClientWithoutCookies();

        for (int attempt = 0; attempt < RaceAttempts; attempt++)
        {
            string token = RefreshCookie.ValueFrom(await Client.LoginAsync(Email, Password));

            HttpResponseMessage[] responses = await Task.WhenAll(
                loggingOut.PostWithRefreshTokenAsync(AuthApi.LogoutAllRoute, token),
                refreshing.PostWithRefreshTokenAsync(AuthApi.RefreshRoute, token));

            HttpResponseMessage logoutAll = responses[0];
            HttpResponseMessage refresh = responses[1];
            if (logoutAll.StatusCode == HttpStatusCode.NoContent && refresh.StatusCode == HttpStatusCode.OK)
            {
                string rotatedToken = RefreshCookie.ValueFrom(refresh);
                (await refreshing.PostWithRefreshTokenAsync(AuthApi.RefreshRoute, rotatedToken))
                    .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            }
        }
    }

    private async Task ExpireOldestRefreshTokenAsync()
    {
        Guid oldestTokenId = await QueryDatabaseAsync(db => db.RefreshTokens
            .OrderBy(token => token.CreatedAt)
            .Select(token => token.Id)
            .FirstAsync(CancellationToken));

        await QueryDatabaseAsync(db => db.RefreshTokens
            .Where(token => token.Id == oldestTokenId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)),
                CancellationToken));
    }
}
