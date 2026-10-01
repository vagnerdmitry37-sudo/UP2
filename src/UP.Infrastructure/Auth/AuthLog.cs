using Microsoft.Extensions.Logging;

namespace UP.Infrastructure.Auth;

internal static partial class AuthLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Registered user {UserId}")]
    public static partial void LogUserRegistered(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Registration attempted for an email that is already taken")]
    public static partial void LogDuplicateRegistration(this ILogger logger);

    [LoggerMessage(EventId = 1020, Level = LogLevel.Information, Message = "User {UserId} logged in")]
    public static partial void LogUserLoggedIn(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1021, Level = LogLevel.Information, Message = "Login attempted for an unknown email")]
    public static partial void LogLoginUnknownEmail(this ILogger logger);

    [LoggerMessage(EventId = 1022, Level = LogLevel.Information, Message = "Login failed with an invalid password for user {UserId}")]
    public static partial void LogLoginInvalidPassword(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1023, Level = LogLevel.Information, Message = "Login attempted for locked-out user {UserId}")]
    public static partial void LogLoginLockedOut(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1024, Level = LogLevel.Warning,
        Message = "User {UserId} locked out after too many failed login attempts")]
    public static partial void LogUserLockedOut(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1040, Level = LogLevel.Warning,
        Message = "Refresh token reuse detected for user {UserId}; revoked token family {FamilyId}")]
    public static partial void LogTokenReuse(this ILogger logger, Guid userId, Guid familyId);

    [LoggerMessage(EventId = 1041, Level = LogLevel.Information,
        Message = "Concurrent refresh lost the race for user {UserId}, token {TokenId}")]
    public static partial void LogRotationRace(this ILogger logger, Guid userId, Guid tokenId);

    [LoggerMessage(EventId = 1060, Level = LogLevel.Information,
        Message = "User {UserId} logged out; revoked token family {FamilyId}")]
    public static partial void LogUserLoggedOut(this ILogger logger, Guid userId, Guid familyId);
}
