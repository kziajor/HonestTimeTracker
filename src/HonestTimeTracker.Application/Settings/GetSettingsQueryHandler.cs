namespace HonestTimeTracker.Application.Settings;

public class GetSettingsQueryHandler(ISettingsRepository repository)
    : IQueryHandler<GetSettingsQuery, SettingsDto>
{
    public async Task<SettingsDto> HandleAsync(GetSettingsQuery query, CancellationToken ct = default)
    {
        var settings = await repository.GetAsync(ct);
        return new SettingsDto(settings.DbFilePath, settings.DailyWorkHours, settings.DefaultTaskPlannedHours);
    }
}
