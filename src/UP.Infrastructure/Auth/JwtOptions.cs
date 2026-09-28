using System.Text;

using Microsoft.IdentityModel.Tokens;

namespace UP.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Options:Jwt";

    public string Key { get; init; } = string.Empty;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; init; } = 15;

    internal SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(Key));
}
