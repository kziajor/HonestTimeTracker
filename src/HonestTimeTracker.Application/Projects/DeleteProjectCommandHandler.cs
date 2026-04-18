namespace HonestTimeTracker.Application.Projects;

public class DeleteProjectCommandHandler(IProjectRepository repository)
    : ICommandHandler<DeleteProjectCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteProjectCommand command, CancellationToken ct = default)
    {
        var project = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Projekt o ID {command.Id} nie istnieje.");

        project.IsDeleted = true;
        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
