namespace HonestTimeTracker.Application.Projects;

public record GetProjectsQuery(bool ShowClosed = false) : IQuery<List<ProjectDto>>;
