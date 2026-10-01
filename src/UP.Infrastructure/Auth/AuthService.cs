using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using UP.Application.Auth;
using UP.Infrastructure.Identity;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.Auth;

internal sealed class AuthService(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAccessTokenGenerator accessTokenGenerator,
    RefreshTokenFactory refreshTokenFactory,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    private static readonly string DummyPasswordHash =
        new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), "dummy-password-for-timing");

    public async Task<RegistrationResult> RegisterAsync(string email, string password)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        var user = new ApplicationUser { Id = Guid.CreateVersion7(now), UserName = email, Email = email, CreatedAt = now };

        IdentityResult result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            logger.LogUserRegistered(user.Id);
            return RegistrationResult.Success;
        }

        if (result.Errors.Any(IsDuplicate))
        {
            logger.LogDuplicateRegistration();
            return RegistrationResult.Success;
        }

        return RegistrationResult.Failure(result.Errors.Select(error => error.Description));
    }

    public async Task<AuthTokens?> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            VerifyDummyPassword(password);
            logger.LogLoginUnknownEmail();
            return null;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            VerifyDummyPassword(password);
            logger.LogLoginLockedOut(user.Id);
            return null;
        }

        bool isPasswordValid = await userManager.CheckPasswordAsync(user, password);

        if (!isPasswordValid)
        {
            await userManager.AccessFailedAsync(user);

            if (await userManager.IsLockedOutAsync(user))
            {
                logger.LogUserLockedOut(user.Id);
            }
            else
            {
                logger.LogLoginInvalidPassword(user.Id);
            }

            return null;
        }

        await userManager.ResetAccessFailedCountAsync(user);

        DateTimeOffset now = timeProvider.GetUtcNow();
        (RefreshToken refreshToken, IssuedRefreshToken issued) = refreshTokenFactory.Create(user.Id, familyId: Guid.CreateVersion7(now));

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        IList<string> roles = await userManager.GetRolesAsync(user);
        AccessToken accessToken = accessTokenGenerator.Generate(user.Id, user.Email ?? string.Empty, [.. roles]);

        logger.LogUserLoggedIn(user.Id);
        return new AuthTokens(accessToken, issued);
    }

    private void VerifyDummyPassword(string password) =>
        userManager.PasswordHasher.VerifyHashedPassword(new ApplicationUser(), DummyPasswordHash, password);

    private static bool IsDuplicate(IdentityError error) =>
        error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName);
}
