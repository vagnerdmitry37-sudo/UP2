using UP.Domain.HistoryRecords;

namespace UP.Application.HistoryRecords;

public interface IHistoryRecordService
{
    Task<IReadOnlyList<HistoryRecord>> GetAllAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken);
}
