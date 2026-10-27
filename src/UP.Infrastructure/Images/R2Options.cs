using Microsoft.Extensions.Configuration;

namespace UP.Infrastructure.Images;

public sealed class R2Options
{
    public const string SectionName = "Options:R2";

    [ConfigurationKeyName("Default")]
    public string ServiceUrl { get; init; } = string.Empty;

    [ConfigurationKeyName("AccessKeyID")]
    public string AccessKeyId { get; init; } = string.Empty;

    public string SecretAccessKey { get; init; } = string.Empty;

    public string Bucket { get; init; } = string.Empty;

    public string PublicBaseUrl { get; init; } = string.Empty;
}
