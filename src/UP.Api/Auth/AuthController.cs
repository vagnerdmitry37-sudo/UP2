using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using UP.Application.Auth;

namespace UP.Api.Auth;

[ApiController]
[Route(AuthRoutes.Base)]
public sealed class AuthController(IAuthService authService, IRefreshTokenService refreshTokenService)
    : ControllerBase
{
    [HttpPost(AuthRoutes.Register)]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        RegistrationResult result = await authService.RegisterAsync(request.Email, request.Password);

        if (result.Succeeded)
        {
            return Accepted();
        }

        foreach (string error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return ValidationProblem(ModelState);
    }

    [HttpPost(AuthRoutes.Login)]
    [AllowAnonymous]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccessTokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        AuthTokens? tokens = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        if (tokens is null)
        {
            return Unauthorized();
        }

        RefreshTokenCookie.Append(Response, tokens.RefreshToken);

        return Ok(new AccessTokenResponse(tokens.AccessToken.Value, tokens.AccessToken.ExpiresAt));
    }

    [HttpPost(AuthRoutes.Refresh)]
    [AllowAnonymous]
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

    [HttpPost(AuthRoutes.Logout)]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        string? refreshToken = RefreshTokenCookie.Read(Request);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return NoContent();
        }

        await refreshTokenService.RevokeFamilyAsync(refreshToken, CancellationToken.None);
        RefreshTokenCookie.Delete(Response);

        return NoContent();
    }

    [HttpPost(AuthRoutes.LogoutAll)]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll()
    {
        string? refreshToken = RefreshTokenCookie.Read(Request);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized();
        }

        if (!await refreshTokenService.RevokeAllAsync(refreshToken, CancellationToken.None))
        {
            return Unauthorized();
        }

        RefreshTokenCookie.Delete(Response);

        return NoContent();
    }

    [HttpGet(AuthRoutes.Me)]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<MeResponse> Me()
    {
        return User.TryGetUserId(out Guid userId) && User.GetEmail() is { Length: > 0 } email
            ? Ok(new MeResponse(userId, email))
            : Unauthorized();
    }
}
