namespace HonestTimeTracker.Application.Settings;

public record UpdateSettingsCommand(
    double DailyWorkHours,
    double? DefaultTaskPlannedHours) : ICommand<Unit>;
