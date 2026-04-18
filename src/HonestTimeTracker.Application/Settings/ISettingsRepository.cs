using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Settings;

public interface ISettingsRepository
{
    Task<AppSettings> GetAsync(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
