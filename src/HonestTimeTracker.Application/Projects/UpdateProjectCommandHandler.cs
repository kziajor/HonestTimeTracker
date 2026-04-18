namespace HonestTimeTracker.Application.Projects;

public class UpdateProjectCommandHandler(IProjectRepository repository)
    : ICommandHandler<UpdateProjectCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateProjectCommand command, CancellationToken ct = default)
    {
        var project = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Projekt o ID {command.Id} nie istnieje.");

        project.Name = command.Name.Trim();
        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
