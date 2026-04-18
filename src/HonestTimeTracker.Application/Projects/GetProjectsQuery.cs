namespace HonestTimeTracker.Application.Projects;

public record GetProjectsQuery(bool IncludeClosed = true) : IQuery<List<ProjectDto>>;
