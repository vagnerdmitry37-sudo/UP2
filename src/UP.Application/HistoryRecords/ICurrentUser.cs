namespace UP.Application.HistoryRecords;

public interface ICurrentUser
{
    Guid? UserId { get; }
}
