using System.Net.Http.Headers;
using System.Net.Http.Json;

using UP.Api.Auth;

namespace UP.Api.IntegrationTests.Auth;

internal static class AuthApi
{
    public const string RegisterRoute = "api/auth/register";
    public const string LoginRoute = "api/auth/login";
    public const string RefreshRoute = "api/auth/refresh";
    public const string LogoutRoute = "api/auth/logout";
    public const string LogoutAllRoute = "api/auth/logout-all";
    public const string MeRoute = "api/auth/me";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email, string password) =>
        client.PostAsJsonAsync(RegisterRoute, new { email, password }, CancellationToken);

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password) =>
        client.PostAsJsonAsync(LoginRoute, new { email, password }, CancellationToken);

    public static async Task<AccessTokenResponse> RegisterAndLoginAsync(
        this HttpClient client,
        string email,
        string password)
    {
        await client.RegisterAsync(email, password);
        HttpResponseMessage response = await client.LoginAsync(email, password);
        response.EnsureSuccessStatusCode();
        return await response.ReadAccessTokenAsync();
    }

    public static Task<HttpResponseMessage> RefreshAsync(this HttpClient client) =>
        client.PostAsync(RefreshRoute, content: null, CancellationToken);

    public static Task<HttpResponseMessage> LogoutAsync(this HttpClient client) =>
        client.PostAsync(LogoutRoute, content: null, CancellationToken);

    public static Task<HttpResponseMessage> LogoutAllAsync(this HttpClient client) =>
        client.PostAsync(LogoutAllRoute, content: null, CancellationToken);

    public static async Task<HttpResponseMessage> PostWithRefreshTokenAsync(
        this HttpClient client,
        string route,
        string refreshToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route);
        request.Headers.Add("Cookie", $"{RefreshCookie.Name}={refreshToken}");
        return await client.SendAsync(request, CancellationToken);
    }

    public static Task<HttpResponseMessage> GetMeAsync(this HttpClient client, string accessToken) =>
        client.GetMeAsync(new AuthenticationHeaderValue("Bearer", accessToken));

    public static async Task<HttpResponseMessage> GetMeAsync(
        this HttpClient client,
        AuthenticationHeaderValue? authorization = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, MeRoute);
        request.Headers.Authorization = authorization;
        return await client.SendAsync(request, CancellationToken);
    }

    public static async Task<AccessTokenResponse> ReadAccessTokenAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<AccessTokenResponse>(CancellationToken)
        ?? throw new InvalidOperationException("Response has no access token.");
}
