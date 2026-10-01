using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

using UP.Application.Auth;
using UP.Infrastructure.Identity;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.Auth;

internal sealed class RefreshTokenService(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAccessTokenGenerator accessTokenGenerator,
    RefreshTokenFactory refreshTokenFactory,
    TimeProvider timeProvider,
    ILogger<RefreshTokenService> logger) : IRefreshTokenService
{
    public async Task<AuthTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        string tokenHash = RefreshTokenFactory.Hash(refreshToken);
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
            logger.LogTokenReuse(current.UserId, current.FamilyId);
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

        (RefreshToken replacement, IssuedRefreshToken issued) = refreshTokenFactory.Create(current.UserId, current.FamilyId);

        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        bool rotated = await strategy.ExecuteAsync(() => RotateRefreshToken(current.Id, replacement, now, cancellationToken));

        if (!rotated)
        {
            logger.LogRotationRace(current.UserId, current.Id);
            return null;
        }

        IList<string> roles = await userManager.GetRolesAsync(user);
        AccessToken accessToken = accessTokenGenerator.Generate(user.Id, user.Email ?? string.Empty, [.. roles]);

        return new AuthTokens(accessToken, issued);
    }

    public async Task RevokeFamilyAsync(string refreshToken, CancellationToken cancellationToken)
    {
        string tokenHash = RefreshTokenFactory.Hash(refreshToken);
        RefreshToken? current = await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (current is null)
        {
            return;
        }

        if (current.IsRevoked)
        {
            logger.LogTokenReuse(current.UserId, current.FamilyId);
        }

        await RevokeFamilyAsync(current.FamilyId, timeProvider.GetUtcNow(), cancellationToken);
        logger.LogUserLoggedOut(current.UserId, current.FamilyId);
    }

    private async Task<bool> RotateRefreshToken(
        Guid currentRefreshTokenId,
        RefreshToken replacement,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await LockFamilyAsync(replacement.FamilyId, cancellationToken);

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

    private Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await LockFamilyAsync(familyId, cancellationToken);

            await dbContext.RefreshTokens
                .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    // Serializes rotation and revocation per family. Without it, a revocation running under READ COMMITTED
    // cannot see the replacement row inserted by a concurrent rotation, leaving that token live after logout.
    private Task<int> LockFamilyAsync(Guid familyId, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({familyId.ToString()}, 0))",
            cancellationToken);
}
