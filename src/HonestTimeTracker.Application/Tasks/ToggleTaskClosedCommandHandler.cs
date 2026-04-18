namespace HonestTimeTracker.Application.Tasks;

public class ToggleTaskClosedCommandHandler(ITaskRepository repository)
    : ICommandHandler<ToggleTaskClosedCommand, Unit>
{
    public async Task<Unit> HandleAsync(ToggleTaskClosedCommand command, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Task with ID {command.Id} does not exist.");

        task.Closed = !task.Closed;
        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
