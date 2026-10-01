using UP.Application.Auth;

namespace UP.Api.Auth;

internal static class RefreshTokenCookie
{
    public const string Name = "__Secure-refresh_token";

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Append(HttpResponse response, IssuedRefreshToken token) =>
        response.Cookies.Append(Name, token.Value, CreateOptions(token.ExpiresAt));

    public static void Delete(HttpResponse response) =>
        response.Cookies.Delete(Name, CreateOptions(expires: null));

    private static CookieOptions CreateOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/" + AuthRoutes.Base,
        Expires = expires,
        IsEssential = true,
    };
}
