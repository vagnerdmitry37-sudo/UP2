using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

using UP.Application.Auth;

namespace UP.Infrastructure.Auth;

internal sealed class RefreshTokenFactory(IOptions<RefreshTokenOptions> options, TimeProvider timeProvider)
{
    private const int TokenSizeInBytes = 64;

    private readonly RefreshTokenOptions _options = options.Value;

    public (RefreshToken Entity, IssuedRefreshToken Issued) Create(Guid userId, Guid familyId)
    {
        string rawToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenSizeInBytes));
        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset expiresAt = now.AddDays(_options.LifetimeDays);

        var entity = new RefreshToken
        {
            Id = Guid.CreateVersion7(now),
            UserId = userId,
            TokenHash = Hash(rawToken),
            FamilyId = familyId,
            CreatedAt = now,
            ExpiresAt = expiresAt,
        };

        return (entity, new IssuedRefreshToken(rawToken, expiresAt));
    }

    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
