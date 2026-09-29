using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using UP.Application.Auth;
using UP.Infrastructure.Identity;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.Auth;

internal sealed partial class RefreshTokenService(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAccessTokenGenerator accessTokenGenerator,
    IOptions<RefreshTokenOptions> options,
    TimeProvider timeProvider,
    ILogger<RefreshTokenService> logger) : IRefreshTokenService
{
    private const int TokenSizeInBytes = 64;

    private readonly RefreshTokenOptions _options = options.Value;

    public async Task<AuthTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        string tokenHash = Hash(refreshToken);
        RefreshToken? current = await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (current is null)
        {
            return null;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        if (current.IsRevoked)
        {
            LogTokenReuse(logger, current.UserId, current.FamilyId);
            await RevokeFamilyAsync(current.FamilyId, now, cancellationToken);
            return null;
        }

        if (current.IsExpired(now))
        {
            return null;
        }

        ApplicationUser? user = await userManager.FindByIdAsync(current.UserId.ToString());
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            await RevokeFamilyAsync(current.FamilyId, now, cancellationToken);
            return null;
        }

        (RefreshToken replacement, IssuedRefreshToken issued) = CreateRefreshToken(current.UserId, current.FamilyId);

        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        bool rotated = await strategy.ExecuteAsync(() => RotateRefreshToken(current.Id, replacement, now, cancellationToken));

        if (!rotated)
        {
            LogRotationRace(logger, current.UserId, current.Id);
            return null;
        }

        IList<string> roles = await userManager.GetRolesAsync(user);
        AccessToken accessToken = accessTokenGenerator.Generate(user.Id, user.Email ?? string.Empty, [.. roles]);

        return new AuthTokens(accessToken, issued);
    }

    private async Task<bool> RotateRefreshToken(
        Guid currentRefreshTokenId,
        RefreshToken replacement,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        int affected = await dbContext.RefreshTokens
            .Where(token => token.Id == currentRefreshTokenId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.RevokedAt, now)
                .SetProperty(token => token.ReplacedByTokenId, replacement.Id),
                cancellationToken);

        if (affected != 1)
        {
            return false;
        }

        dbContext.RefreshTokens.Add(replacement);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private (RefreshToken Entity, IssuedRefreshToken Issued) CreateRefreshToken(Guid userId, Guid familyId)
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

    private Task<int> RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Refresh token reuse detected for user {UserId}; revoked token family {FamilyId}")]
    private static partial void LogTokenReuse(ILogger logger, Guid userId, Guid familyId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Concurrent refresh lost the race for user {UserId}, token {TokenId}")]
    private static partial void LogRotationRace(ILogger logger, Guid userId, Guid tokenId);
}
