using HonestTimeTracker.Application.Records;
using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Tasks;

public interface ITaskRepository
{
    Task<List<WorkTask>> GetAllAsync(bool showClosed, int? projectId, CancellationToken ct);
    Task<List<WorkTask>> GetTodayAsync(CancellationToken ct);
    Task<WorkTask?> GetByIdAsync(int id, CancellationToken ct);
    Task AddAsync(WorkTask task, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public class GetTasksQueryHandler(ITaskRepository repository, IRecordRepository recordRepository)
    : IQueryHandler<GetTasksQuery, List<TaskDto>>
{
    public async Task<List<TaskDto>> HandleAsync(GetTasksQuery query, CancellationToken ct = default)
    {
        var tasks = await repository.GetAllAsync(query.ShowClosed, query.ProjectId, ct);
        var activeRecord = await recordRepository.GetActiveAsync(ct);
        return tasks
            .Select(t => new TaskDto(t.Id, t.Title, t.PlannedMinutes, t.SpentMinutes, t.Closed, t.ProjectId, t.Project?.Name,
                HasActiveTimer: activeRecord?.TaskId == t.Id,
                IsOnTodayList: t.IsOnTodayList))
            .ToList();
    }
}
