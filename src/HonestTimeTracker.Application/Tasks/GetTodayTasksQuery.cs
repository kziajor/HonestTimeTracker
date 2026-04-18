using HonestTimeTracker.Application.Records;

namespace HonestTimeTracker.Application.Tasks;

public record GetTodayTasksQuery : IQuery<List<TaskDto>>;

public class GetTodayTasksQueryHandler(ITaskRepository repository, IRecordRepository recordRepository)
    : IQueryHandler<GetTodayTasksQuery, List<TaskDto>>
{
    public async Task<List<TaskDto>> HandleAsync(GetTodayTasksQuery query, CancellationToken ct = default)
    {
        var tasks = await repository.GetTodayAsync(ct);
        var activeRecord = await recordRepository.GetActiveAsync(ct);
        return tasks
            .Select(t => new TaskDto(t.Id, t.Title, t.PlannedMinutes, t.SpentMinutes, t.Closed, t.ProjectId, t.Project?.Name,
                HasActiveTimer: activeRecord?.TaskId == t.Id,
                IsOnTodayList: true))
            .ToList();
    }
}
