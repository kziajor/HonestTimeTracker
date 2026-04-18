using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Tasks;

public interface ITaskRepository
{
    Task<List<WorkTask>> GetAllAsync(bool showClosed, int? projectId, CancellationToken ct);
    Task<WorkTask?> GetByIdAsync(int id, CancellationToken ct);
    Task AddAsync(WorkTask task, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public class GetTasksQueryHandler(ITaskRepository repository)
    : IQueryHandler<GetTasksQuery, List<TaskDto>>
{
    public async Task<List<TaskDto>> HandleAsync(GetTasksQuery query, CancellationToken ct = default)
    {
        var tasks = await repository.GetAllAsync(query.ShowClosed, query.ProjectId, ct);
        return tasks
            .Select(t => new TaskDto(t.Id, t.Title, t.PlannedMinutes, t.SpentMinutes, t.Closed, t.ProjectId, t.Project?.Name))
            .ToList();
    }
}
