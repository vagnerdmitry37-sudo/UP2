using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;

namespace UP.Api.Auth;

internal static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal user, out Guid userId) =>
        Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId);

    public static string? GetEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Email);
}
