namespace HonestTimeTracker.Application.Projects;

public class ToggleProjectClosedCommandHandler(IProjectRepository repository)
    : ICommandHandler<ToggleProjectClosedCommand, Unit>
{
    public async Task<Unit> HandleAsync(ToggleProjectClosedCommand command, CancellationToken ct = default)
    {
        var project = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Projekt o ID {command.Id} nie istnieje.");

        project.Closed = !project.Closed;
        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
