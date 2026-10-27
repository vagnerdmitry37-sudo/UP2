using System.Text.Json;

using Riok.Mapperly.Abstractions;

using UP.Domain.HistoryRecords;

namespace UP.Api.HistoryRecords;

[Mapper]
internal static partial class HistoryRecordMapper
{
    [MapProperty(nameof(HistoryRecord.Changes), nameof(HistoryRecordResponse.Changes), Use = nameof(ParseChanges))]
    public static partial HistoryRecordResponse ToResponse(this HistoryRecord record);

    public static partial IReadOnlyList<HistoryRecordResponse> ToResponses(this IReadOnlyList<HistoryRecord> records);

    private static JsonElement ParseChanges(string changes) => JsonSerializer.Deserialize<JsonElement>(changes);
}
