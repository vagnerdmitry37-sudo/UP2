using Riok.Mapperly.Abstractions;

using UP.Domain.UserProfiles;

namespace UP.Api.UserProfiles;

[Mapper]
internal static partial class UserProfileMapper
{
    [MapperIgnoreTarget(nameof(UserProfile.Id))]
    public static partial UserProfile ToProfile(this UpdateUserProfileRequest request);

    [MapperIgnoreSource(nameof(UserProfile.Id))]
    public static partial UserProfileResponse ToResponse(this UserProfile profile);
}
