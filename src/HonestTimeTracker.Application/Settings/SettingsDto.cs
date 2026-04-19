namespace HonestTimeTracker.Application.Settings;

public record SettingsDto(
    string DbFilePath,
    double DailyWorkHours,
    double? DefaultTaskPlannedHours,
    bool ShowFloatingTimer,
    double? FloatingTimerLeft,
    double? FloatingTimerTop);
