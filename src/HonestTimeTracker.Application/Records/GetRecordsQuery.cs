using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Records;

public interface IRecordRepository
{
    Task<List<WorkRecord>> GetAllAsync(int? taskId, DateOnly? date, CancellationToken ct);
    Task<WorkRecord?> GetByIdAsync(int id, CancellationToken ct);
    Task<WorkRecord?> GetActiveAsync(CancellationToken ct);
    Task AddAsync(WorkRecord record, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public record GetRecordsQuery(int? TaskId = null, DateOnly? Date = null) : IQuery<List<RecordDto>>;

public class GetRecordsQueryHandler(IRecordRepository repository)
    : IQueryHandler<GetRecordsQuery, List<RecordDto>>
{
    public async Task<List<RecordDto>> HandleAsync(GetRecordsQuery query, CancellationToken ct = default)
    {
        var records = await repository.GetAllAsync(query.TaskId, query.Date, ct);
        return records
            .Select(r => new RecordDto(
                r.Id,
                r.TaskId,
                r.Task.Title,
                r.Task.Project?.Name,
                r.StartedAt,
                r.FinishedAt,
                r.MinutesSpent,
                r.Comment))
            .ToList();
    }
}
