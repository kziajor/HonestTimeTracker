namespace HonestTimeTracker.Application.Projects;

public record ProjectDto(int Id, string Name, bool Closed, string? TfsProjectId, int? TfsCollectionId);
