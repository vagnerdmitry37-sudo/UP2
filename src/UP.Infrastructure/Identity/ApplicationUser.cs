using Microsoft.AspNetCore.Identity;

namespace UP.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
