using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Projects;

public interface IProjectRepository
{
    Task<List<Project>> GetAllAsync(bool showClosed, CancellationToken ct);
    Task<Project?> GetByIdAsync(int id, CancellationToken ct);
    Task AddAsync(Project project, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public class GetProjectsQueryHandler(IProjectRepository repository)
    : IQueryHandler<GetProjectsQuery, List<ProjectDto>>
{
    public async Task<List<ProjectDto>> HandleAsync(GetProjectsQuery query, CancellationToken ct = default)
    {
        var projects = await repository.GetAllAsync(query.ShowClosed, ct);
        return projects
            .Select(p => new ProjectDto(p.Id, p.Name, p.Closed, p.TfsProjectId, p.TfsCollectionId))
            .ToList();
    }
}
