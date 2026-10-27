using Microsoft.EntityFrameworkCore;

using UP.Application.HistoryRecords;
using UP.Domain.HistoryRecords;
using UP.Infrastructure.Persistence;

namespace UP.Infrastructure.HistoryRecords;

internal sealed class HistoryRecordService(AppDbContext dbContext) : IHistoryRecordService
{
    public async Task<IReadOnlyList<HistoryRecord>> GetAllAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken) =>
        await dbContext.HistoryRecords
            .AsNoTracking()
            .Where(record => record.EntityType == entityType && record.EntityId == entityId)
            .OrderBy(record => record.OccurredAt)
            .ThenBy(record => record.Id)
            .ToListAsync(cancellationToken);
}
