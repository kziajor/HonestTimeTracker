using HonestTimeTracker.Application.Settings;
using HonestTimeTracker.Domain;
using HonestTimeTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HonestTimeTracker.Infrastructure.Repositories;

public class SettingsRepository(AppDbContext db) : ISettingsRepository
{
    public async Task<AppSettings> GetAsync(CancellationToken ct)
    {
        var settings = await db.Settings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new AppSettings { Id = 1 };
            await db.Settings.AddAsync(settings, ct);
            await db.SaveChangesAsync(ct);
        }
        return settings;
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
