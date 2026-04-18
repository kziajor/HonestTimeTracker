namespace HonestTimeTracker.Application.Tasks;

public class DeleteTaskCommandHandler(ITaskRepository repository)
    : ICommandHandler<DeleteTaskCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteTaskCommand command, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Task with ID {command.Id} does not exist.");

        task.IsDeleted = true;
        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
