using Microsoft.Net.Http.Headers;

namespace UP.Api.IntegrationTests.Auth;

internal static class RefreshCookie
{
    public const string Name = "__Secure-refresh_token";

    public static SetCookieHeaderValue? Find(HttpResponseMessage response) =>
        response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string>? headers)
            ? SetCookieHeaderValue.ParseList([.. headers]).SingleOrDefault(cookie => cookie.Name == Name)
            : null;

    public static string ValueFrom(HttpResponseMessage response) =>
        Find(response)?.Value.Value
        ?? throw new InvalidOperationException("Response did not set refresh cookie.");

    public static bool IsDeletedBy(HttpResponseMessage response) =>
        Find(response) is { Expires: { } expires } && expires < DateTimeOffset.UtcNow;
}
