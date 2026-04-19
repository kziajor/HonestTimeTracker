namespace HonestTimeTracker.Application.Settings;

public record UpdateSettingsCommand(
    double DailyWorkHours,
    double? DefaultTaskPlannedHours,
    bool ShowFloatingTimer) : ICommand<Unit>;
