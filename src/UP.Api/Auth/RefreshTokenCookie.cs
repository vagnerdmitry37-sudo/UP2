using UP.Application.Auth;

namespace UP.Api.Auth;

internal static class RefreshTokenCookie
{
    public const string Name = "__Secure-refresh_token";

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Append(HttpResponse response, IssuedRefreshToken token) =>
        response.Cookies.Append(Name, token.Value, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = AuthRoutes.Refresh,
            Expires = token.ExpiresAt,
            IsEssential = true,
        });
}
