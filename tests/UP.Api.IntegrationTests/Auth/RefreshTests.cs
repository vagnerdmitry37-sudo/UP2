using System.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

using UP.Api.Auth;
using UP.Api.IntegrationTests.TestHost;

namespace UP.Api.IntegrationTests.Auth;

public sealed class RefreshTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string Email = "alice@example.com";
    private const string Password = "correct-horse-battery";
    private const int RaceAttempts = 5;

    [Fact]
    public async Task Refresh_without_session_is_rejected()
    {
        HttpResponseMessage response = await Client.RefreshAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_with_unknown_token_is_rejected()
    {
        HttpResponseMessage response = await CreateClientWithoutCookies()
            .PostWithRefreshTokenAsync(AuthApi.RefreshRoute, "not-a-real-refresh-token");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_issues_new_access_token_and_new_refresh_token()
    {
        await Client.RegisterAsync(Email, Password);
        HttpResponseMessage login = await Client.LoginAsync(Email, Password);

        HttpResponseMessage refresh = await Client.RefreshAsync();

        refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await refresh.ReadAccessTokenAsync()).AccessToken.ShouldNotBe((await login.ReadAccessTokenAsync()).AccessToken);
        RefreshCookie.ValueFrom(refresh).ShouldNotBe(RefreshCookie.ValueFrom(login));
    }

    [Fact]
    public async Task Refreshed_refresh_cookie_keeps_security_attributes()
    {
        await Client.RegisterAndLoginAsync(Email, Password);

        HttpResponseMessage refresh = await Client.RefreshAsync();

        SetCookieHeaderValue cookie = RefreshCookie.Find(refresh).ShouldNotBeNull();
        cookie.HttpOnly.ShouldBeTrue();
        cookie.Secure.ShouldBeTrue();
        cookie.SameSite.ShouldBe(SameSiteMode.Strict);
        cookie.Path.Value.ShouldBe("/api/auth");
    }

    [Fact]
    public async Task Refreshed_access_token_grants_access_to_current_user()
    {
        await Client.RegisterAndLoginAsync(Email, Password);
        AccessTokenResponse token = await (await Client.RefreshAsync()).ReadAccessTokenAsync();

        HttpResponseMessage me = await Client.GetMeAsync(token.AccessToken);

        me.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Session_can_be_refreshed_repeatedly()
    {
        await Client.RegisterAndLoginAsync(Email, Password);

        for (int refresh = 0; refresh < 3; refresh++)
        {
            (await Client.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Reused_refresh_token_is_rejected()
    {
        await Client.RegisterAsync(Email, Password);
        string oldToken = RefreshCookie.ValueFrom(await Client.LoginAsync(Email, Password));
        await Client.RefreshAsync();

        HttpResponseMessage reuse = await CreateClientWithoutCookies()
            .PostWithRefreshTokenAsync(AuthApi.RefreshRoute, oldToken);

        reuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reused_refresh_token_ends_session_for_everyone_holding_it()
    {
        await Client.RegisterAsync(Email, Password);
        string stolenToken = RefreshCookie.ValueFrom(await Client.LoginAsync(Email, Password));
        await Client.RefreshAsync();

        await CreateClientWithoutCookies().PostWithRefreshTokenAsync(AuthApi.RefreshRoute, stolenToken);

        (await Client.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reused_refresh_token_keeps_other_sessions_alive()
    {
        HttpClient phone = CreateSessionClient();
        await Client.RegisterAsync(Email, Password);
        string stolenToken = RefreshCookie.ValueFrom(await Client.LoginAsync(Email, Password));
        await phone.LoginAsync(Email, Password);
        await Client.RefreshAsync();

        await CreateClientWithoutCookies().PostWithRefreshTokenAsync(AuthApi.RefreshRoute, stolenToken);

        (await phone.RefreshAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected()
    {
        await Client.RegisterAndLoginAsync(Email, Password);
        await ExpireAllRefreshTokensAsync();

        HttpResponseMessage response = await Client.RefreshAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Failed_refresh_leaves_refresh_cookie_untouched()
    {
        await Client.RegisterAndLoginAsync(Email, Password);
        await ExpireAllRefreshTokensAsync();

        HttpResponseMessage response = await Client.RefreshAsync();

        RefreshCookie.Find(response).ShouldBeNull();
    }

    [Fact]
    public async Task Parallel_refreshes_with_same_token_let_only_one_succeed()
    {
        await Client.RegisterAsync(Email, Password);
        HttpClient first = CreateClientWithoutCookies();
        HttpClient second = CreateClientWithoutCookies();

        for (int attempt = 0; attempt < RaceAttempts; attempt++)
        {
            string token = RefreshCookie.ValueFrom(await Client.LoginAsync(Email, Password));

            HttpResponseMessage[] responses = await Task.WhenAll(
                first.PostWithRefreshTokenAsync(AuthApi.RefreshRoute, token),
                second.PostWithRefreshTokenAsync(AuthApi.RefreshRoute, token));

            responses.Count(response => response.StatusCode == HttpStatusCode.OK).ShouldBe(1);
            responses.Count(response => response.StatusCode == HttpStatusCode.Unauthorized).ShouldBe(1);
        }
    }

    private Task<int> ExpireAllRefreshTokensAsync() =>
        QueryDatabaseAsync(db => db.RefreshTokens.ExecuteUpdateAsync(
            setters => setters.SetProperty(token => token.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)),
            CancellationToken));
}
