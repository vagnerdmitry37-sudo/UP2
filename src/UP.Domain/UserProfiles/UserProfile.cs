namespace UP.Domain.UserProfiles;

public sealed class UserProfile
{
    public Guid Id { get; set; }

    public string Currency { get; set; } = "USD";
}
