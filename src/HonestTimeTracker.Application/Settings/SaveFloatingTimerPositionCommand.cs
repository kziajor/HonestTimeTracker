namespace HonestTimeTracker.Application.Settings;

public record SaveFloatingTimerPositionCommand(double Left, double Top) : ICommand<Unit>;

public class SaveFloatingTimerPositionCommandHandler(ISettingsRepository repository)
    : ICommandHandler<SaveFloatingTimerPositionCommand, Unit>
{
    public async Task<Unit> HandleAsync(SaveFloatingTimerPositionCommand command, CancellationToken ct = default)
    {
        var settings = await repository.GetAsync(ct);
        settings.FloatingTimerLeft = command.Left;
        settings.FloatingTimerTop = command.Top;
        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
