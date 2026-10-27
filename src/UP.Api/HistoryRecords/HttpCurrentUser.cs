using UP.Api.Auth;
using UP.Application.HistoryRecords;

namespace UP.Api.HistoryRecords;

internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId =>
        httpContextAccessor.HttpContext?.User.TryGetUserId(out Guid userId) == true ? userId : null;
}
