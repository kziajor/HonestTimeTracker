namespace HonestTimeTracker.Application.Settings;

public class UpdateSettingsCommandHandler(ISettingsRepository repository)
    : ICommandHandler<UpdateSettingsCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateSettingsCommand command, CancellationToken ct = default)
    {
        var settings = await repository.GetAsync(ct);
        settings.DailyWorkHours = command.DailyWorkHours;
        settings.DefaultTaskPlannedHours = command.DefaultTaskPlannedHours;
        settings.ShowFloatingTimer = command.ShowFloatingTimer;
        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
