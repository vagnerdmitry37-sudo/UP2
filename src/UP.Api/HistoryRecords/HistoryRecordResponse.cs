using System.Text.Json;

using UP.Domain.HistoryRecords;

namespace UP.Api.HistoryRecords;

public sealed record HistoryRecordResponse(
    Guid Id,
    string EntityType,
    Guid EntityId,
    HistoryOperation Operation,
    Guid? UserId,
    DateTimeOffset OccurredAt,
    JsonElement Changes);
