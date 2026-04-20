namespace HonestTimeTracker.Application.Tasks;

public record TaskDto(int Id, int? TfsWorkItemId, string Title, int PlannedMinutes, int SpentMinutes, bool Closed, int? ProjectId, string? ProjectName, bool HasActiveTimer = false, bool IsOnTodayList = false);
