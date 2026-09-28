namespace UP.Infrastructure.Auth;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "Options:RefreshToken";

    public int LifetimeDays { get; init; } = 14;
}
