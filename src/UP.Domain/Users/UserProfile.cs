namespace UP.Domain.Users;

public sealed class UserProfile
{
    public Guid Id { get; set; }

    public string Currency { get; set; } = "USD";
}
