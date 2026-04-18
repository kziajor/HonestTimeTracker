namespace HonestTimeTracker.Application.Tasks;

public record TaskDto(int Id, string Title, int PlannedMinutes, int SpentMinutes, bool Closed, int? ProjectId, string? ProjectName);
