namespace HonestTimeTracker.Application.Records;

public record RecordDto(
    int Id,
    int TaskId,
    string TaskTitle,
    string? ProjectName,
    DateTime StartedAt,
    DateTime FinishedAt,
    int MinutesSpent,
    string? Comment);
