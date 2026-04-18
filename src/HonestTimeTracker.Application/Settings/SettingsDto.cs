namespace HonestTimeTracker.Application.Settings;

public record SettingsDto(
    string DbFilePath,
    double DailyWorkHours,
    double? DefaultTaskPlannedHours);
