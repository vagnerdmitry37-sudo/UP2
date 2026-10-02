using System.Net;

using UP.Api.Auth;
using UP.Api.IntegrationTests.TestHost;

namespace UP.Api.IntegrationTests.Auth;

public sealed class LogoutTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "alice@example.com";
    private const string Password = "correct-horse-battery";
    private const int RaceAttempts = 5;

    [Fact]
    public async Task Logout_ends_session()
    {
        await Client.RegisterAndLoginAsync(Email, Password);

        HttpResponseMessage logout = await Client.LogoutAsync();

        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_deletes_refresh_cookie()
    {
        await Client.RegisterAndLoginAsync(Email, Password);

        HttpResponseMessage logout = await Client.LogoutAsync();

        RefreshCookie.IsDeletedBy(logout).ShouldBeTrue();
    }

    [Fact]
    public async Task Logout_without_session_succeeds_and_leaves_cookies_alone()
    {
        HttpResponseMessage logout = await Client.LogoutAsync();

        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        RefreshCookie.Find(logout).ShouldBeNull();
    }

    [Fact]
    public async Task Logout_with_unknown_token_succeeds()
    {
        HttpResponseMessage logout = await CreateClientWithoutCookies()
            .PostWithRefreshTokenAsync(AuthApi.LogoutRoute, "not-a-real-refresh-token");

        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Repeated_logout_succeeds()
    {
        await Client.RegisterAndLoginAsync(Email, Password);
        await Client.LogoutAsync();

        HttpResponseMessage secondLogout = await Client.LogoutAsync();

        secondLogout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_keeps_other_sessions_alive()
    {
        HttpClient phone = CreateSessionClient();
        await Client.RegisterAndLoginAsync(Email, Password);
        await phone.LoginAsync(Email, Password);

        await Client.LogoutAsync();

        (await phone.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_after_refresh_ends_session()
    {
        await Client.RegisterAndLoginAsync(Email, Password);
        await Client.RefreshAsync();

        await Client.LogoutAsync();

        (await Client.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_with_outdated_refresh_token_still_ends_session()
    {
        await Client.RegisterAsync(Email, Password);
        string outdatedToken = RefreshCookie.ValueFrom(await Client.LoginAsync(Email, Password));
        await Client.RefreshAsync();

        HttpResponseMessage logout = await CreateClientWithoutCookies()
            .PostWithRefreshTokenAsync(AuthApi.LogoutRoute, outdatedToken);

        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Access_token_keeps_working_until_it_expires_after_logout()
    {
        AccessTokenResponse token = await Client.RegisterAndLoginAsync(Email, Password);

        await Client.LogoutAsync();

        (await Client.GetMeAsync(token.AccessToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Parallel_logout_and_refresh_leave_no_usable_session()
    {
        await Client.RegisterAsync(Email, Password);
        HttpClient loggingOut = CreateClientWithoutCookies();
        HttpClient refreshing = CreateClientWithoutCookies();

        for (int attempt = 0; attempt < RaceAttempts; attempt++)
        {
            string token = RefreshCookie.ValueFrom(await Client.LoginAsync(Email, Password));

            HttpResponseMessage[] responses = await Task.WhenAll(
                loggingOut.PostWithRefreshTokenAsync(AuthApi.LogoutRoute, token),
                refreshing.PostWithRefreshTokenAsync(AuthApi.RefreshRoute, token));

            HttpResponseMessage refresh = responses[1];
            if (refresh.StatusCode == HttpStatusCode.OK)
            {
                string rotatedToken = RefreshCookie.ValueFrom(refresh);
                (await refreshing.PostWithRefreshTokenAsync(AuthApi.RefreshRoute, rotatedToken))
                    .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            }
        }
    }
}
