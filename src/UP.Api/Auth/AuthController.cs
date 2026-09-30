using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using UP.Application.Auth;

namespace UP.Api.Auth;

[ApiController]
[Route(AuthRoutes.Base)]
[AllowAnonymous]
public sealed class AuthController(IRefreshTokenService refreshTokenService) : ControllerBase
{
    [HttpPost(AuthRoutes.Refresh)]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccessTokenResponse>> Refresh(CancellationToken cancellationToken)
    {
        string? refreshToken = RefreshTokenCookie.Read(Request);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized();
        }

        AuthTokens? tokens = await refreshTokenService.RefreshAsync(refreshToken, cancellationToken);
        if (tokens is null)
        {
            return Unauthorized();
        }

        RefreshTokenCookie.Append(Response, tokens.RefreshToken);

        return Ok(new AccessTokenResponse(tokens.AccessToken.Value, tokens.AccessToken.ExpiresAt));
    }
}
