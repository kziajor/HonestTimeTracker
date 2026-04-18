namespace HonestTimeTracker.Application.Projects;

public class UpdateProjectCommandHandler(IProjectRepository repository)
    : ICommandHandler<UpdateProjectCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateProjectCommand command, CancellationToken ct = default)
    {
        var project = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Project with ID {command.Id} does not exist.");

        project.Name = command.Name.Trim();
        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
