namespace UP.Infrastructure.Identity;

public sealed class AdminOptions
{
    public const string SectionName = "Options:Admin";

    public const int MinimumPasswordLength = 12;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    internal bool IsConfigured => !string.IsNullOrWhiteSpace(Email);
}
