namespace HonestTimeTracker.Application.Tasks;

public record GetTasksQuery(bool ShowClosed = false, int? ProjectId = null) : IQuery<List<TaskDto>>;
