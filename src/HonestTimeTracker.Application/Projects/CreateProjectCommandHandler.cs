using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Projects;

public class CreateProjectCommandHandler(IProjectRepository repository)
    : ICommandHandler<CreateProjectCommand, int>
{
    public async Task<int> HandleAsync(CreateProjectCommand command, CancellationToken ct = default)
    {
        var project = new Project { Name = command.Name.Trim() };
        await repository.AddAsync(project, ct);
        await repository.SaveChangesAsync(ct);
        return project.Id;
    }
}
