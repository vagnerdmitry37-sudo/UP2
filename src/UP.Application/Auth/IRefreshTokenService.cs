namespace UP.Application.Auth;

public interface IRefreshTokenService
{
    Task<AuthTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeFamilyAsync(string refreshToken, CancellationToken cancellationToken);
}

public sealed record IssuedRefreshToken(string Value, DateTimeOffset ExpiresAt);

public sealed record AuthTokens(AccessToken AccessToken, IssuedRefreshToken RefreshToken);
