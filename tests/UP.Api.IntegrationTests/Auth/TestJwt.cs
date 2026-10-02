using System.Text;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using UP.Api.IntegrationTests.TestHost;

namespace UP.Api.IntegrationTests.Auth;

internal static class TestJwt
{
    public const string Issuer = "up-api";
    public const string Audience = "up-client";
    public const string OtherSigningKey = "attacker-controlled-signing-key-0123456789abcdef";

    private static readonly JsonWebTokenHandler Handler = new() { SetDefaultTimesOnTokenCreation = false };

    public static string Create(
        Guid? userId,
        string? email,
        DateTimeOffset? expiresAt = null,
        string issuer = Issuer,
        string audience = Audience,
        string? signingKey = ApiFactory.JwtKey)
    {
        DateTimeOffset expires = expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(15);
        DateTimeOffset issuedAt = expires.AddMinutes(-15);

        Dictionary<string, object> claims = [];
        if (userId is { } id)
        {
            claims[JwtRegisteredClaimNames.Sub] = id.ToString();
        }

        if (email is not null)
        {
            claims[JwtRegisteredClaimNames.Email] = email;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Claims = claims,
            Issuer = issuer,
            Audience = audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = signingKey is null
                ? null
                : new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    SecurityAlgorithms.HmacSha256),
        };

        return Handler.CreateToken(descriptor);
    }

    public static string WithPayloadOf(string signedToken, string forgedToken)
    {
        string[] signed = signedToken.Split('.');
        string[] forged = forgedToken.Split('.');
        return $"{signed[0]}.{forged[1]}.{signed[2]}";
    }
}
