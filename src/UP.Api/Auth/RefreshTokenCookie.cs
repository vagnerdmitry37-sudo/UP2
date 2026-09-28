using UP.Application.Auth;

namespace UP.Api.Auth;

internal static class RefreshTokenCookie
{
    // __Host- requires Path=/, so a cookie scoped to /api/auth must use __Secure-.
    public const string Name = "__Secure-refresh_token";
    public const string Path = "/api/auth";

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Append(HttpResponse response, IssuedRefreshToken token) =>
        response.Cookies.Append(Name, token.Value, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = Path,
            Expires = token.ExpiresAt,
            IsEssential = true,
        });
}
