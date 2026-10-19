using Microsoft.EntityFrameworkCore;

using UP.Application.UserProfiles;
using UP.Domain.UserProfiles;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.UserProfiles;

internal sealed class UserProfileService(AppDbContext dbContext) : IUserProfileService
{
    public async Task<UserProfile> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        UserProfile? profile = await dbContext.UserProfiles.FindAsync([userId], cancellationToken);

        return profile ?? new UserProfile { Id = userId };
    }

    public async Task<UserProfile> CreateOrUpdateAsync(UserProfile profile, CancellationToken cancellationToken)
    {
        bool exists = await dbContext.UserProfiles.AnyAsync(saved => saved.Id == profile.Id, cancellationToken);

        if (exists)
        {
            dbContext.UserProfiles.Update(profile);
        }
        else
        {
            dbContext.UserProfiles.Add(profile);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return profile;
    }
}
