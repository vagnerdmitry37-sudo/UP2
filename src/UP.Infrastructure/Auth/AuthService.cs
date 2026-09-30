using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using UP.Application.Auth;
using UP.Infrastructure.Identity;

namespace UP.Infrastructure.Auth;

internal sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
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

    private static bool IsDuplicate(IdentityError error) =>
        error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName);
}
