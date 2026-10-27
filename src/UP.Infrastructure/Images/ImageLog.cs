using Microsoft.Extensions.Logging;

namespace UP.Infrastructure.Images;

internal static partial class ImageLog
{
    [LoggerMessage(EventId = 1100, Level = LogLevel.Warning,
        Message = "Deleted {Count} images from the database but could not delete their files from storage")]
    public static partial void LogImageFilesNotDeleted(this ILogger logger, Exception exception, int count);
}
