using System.Net.Http.Json;

namespace UP.Api.IntegrationTests.Auth;

internal static class AuthApi
{
    public const string RegisterRoute = "api/auth/register";
    public const string LoginRoute = "api/auth/login";

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email, string password) =>
        client.PostAsJsonAsync(RegisterRoute, new { email, password }, TestContext.Current.CancellationToken);

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password) =>
        client.PostAsJsonAsync(LoginRoute, new { email, password }, TestContext.Current.CancellationToken);
}
