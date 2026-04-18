namespace HonestTimeTracker.Application.Projects;

public class DeleteProjectCommandHandler(IProjectRepository repository)
    : ICommandHandler<DeleteProjectCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteProjectCommand command, CancellationToken ct = default)
    {
        var project = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Project with ID {command.Id} does not exist.");

        project.IsDeleted = true;
        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
