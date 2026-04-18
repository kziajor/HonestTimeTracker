namespace HonestTimeTracker.Application.Tasks;

public class UpdateTaskCommandHandler(ITaskRepository repository)
    : ICommandHandler<UpdateTaskCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateTaskCommand command, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Task with ID {command.Id} does not exist.");

        task.Title = command.Title.Trim();
        task.PlannedMinutes = command.PlannedMinutes;
        task.ProjectId = (int?)command.ProjectId;
        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
