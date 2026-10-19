using UP.Domain.UserProfiles;

namespace UP.Application.UserProfiles;

public interface IUserProfileService
{
    Task<UserProfile> GetAsync(Guid userId, CancellationToken cancellationToken);

    Task<UserProfile> CreateOrUpdateAsync(UserProfile profile, CancellationToken cancellationToken);
}
