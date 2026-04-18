using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Tasks;

public class CreateTaskCommandHandler(ITaskRepository repository)
    : ICommandHandler<CreateTaskCommand, int>
{
    public async Task<int> HandleAsync(CreateTaskCommand command, CancellationToken ct = default)
    {
        var task = new WorkTask
        {
            Title = command.Title.Trim(),
            PlannedMinutes = command.PlannedMinutes,
            ProjectId = (int?)command.ProjectId
        };
        await repository.AddAsync(task, ct);
        await repository.SaveChangesAsync(ct);
        return task.Id;
    }
}
