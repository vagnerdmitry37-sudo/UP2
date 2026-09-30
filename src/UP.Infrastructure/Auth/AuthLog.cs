using Microsoft.Extensions.Logging;

namespace UP.Infrastructure.Auth;

internal static partial class AuthLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Registered user {UserId}")]
    public static partial void LogUserRegistered(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Registration attempted for an email that is already taken")]
    public static partial void LogDuplicateRegistration(this ILogger logger);

    [LoggerMessage(EventId = 1040, Level = LogLevel.Warning,
        Message = "Refresh token reuse detected for user {UserId}; revoked token family {FamilyId}")]
    public static partial void LogTokenReuse(this ILogger logger, Guid userId, Guid familyId);

    [LoggerMessage(EventId = 1041, Level = LogLevel.Information,
        Message = "Concurrent refresh lost the race for user {UserId}, token {TokenId}")]
    public static partial void LogRotationRace(this ILogger logger, Guid userId, Guid tokenId);
}
