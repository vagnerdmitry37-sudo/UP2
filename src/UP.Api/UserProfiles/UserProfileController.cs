using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using UP.Api.Auth;
using UP.Application.UserProfiles;
using UP.Domain.UserProfiles;

namespace UP.Api.UserProfiles;

[ApiController]
[Route(UserProfileRoutes.Base)]
[Authorize]
public sealed class UserProfileController(IUserProfileService userProfileService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> Get(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        UserProfile profile = await userProfileService.GetAsync(userId, cancellationToken);

        return Ok(profile.ToResponse());
    }

    [HttpPut]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> Update(
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        UserProfile profile = request.ToProfile();
        profile.Id = userId;

        UserProfile updatedProfile = await userProfileService.CreateOrUpdateAsync(profile, cancellationToken);

        return Ok(updatedProfile.ToResponse());
    }
}
