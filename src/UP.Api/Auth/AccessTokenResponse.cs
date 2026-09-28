namespace UP.Api.Authentication;

public sealed record AccessTokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
