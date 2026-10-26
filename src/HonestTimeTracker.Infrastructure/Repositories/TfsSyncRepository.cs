using HonestTimeTracker.Application.Tfs;
using HonestTimeTracker.Domain;
using HonestTimeTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HonestTimeTracker.Infrastructure.Repositories;

public class TfsSyncRepository(AppDbContext db) : ITfsSyncRepository
{
    public Task<List<TfsCollection>> GetCollectionsAsync(CancellationToken ct) =>
        db.TfsCollections.OrderBy(c => c.Name).ToListAsync(ct);

    public Task<List<WorkTask>> GetOpenLinkedTasksAsync(CancellationToken ct) =>
        db.Tasks.Where(t => !t.Closed && t.TfsWorkItemId != null).ToListAsync(ct);

    public Task<List<TfsTaskLink>> GetAllTaskLinksAsync(CancellationToken ct) =>
        db.Tasks.IgnoreQueryFilters()
            .Where(t => t.TfsWorkItemId != null)
            .Select(t => new TfsTaskLink(t.TfsCollectionId, t.TfsWorkItemId!.Value))
            .ToListAsync(ct);

    public Task<int?> GetActiveTimerTaskIdAsync(CancellationToken ct) =>
        db.Records.Where(r => r.FinishedAt == null)
            .Select(r => (int?)r.TaskId)
            .FirstOrDefaultAsync(ct);

    public Task<List<Project>> GetOpenProjectsAsync(CancellationToken ct) =>
        db.Projects.Where(p => !p.Closed).ToListAsync(ct);

    public Task<List<TfsImportPreference>> GetImportPreferencesAsync(CancellationToken ct) =>
        db.TfsImportPreferences.ToListAsync(ct);

    public async Task AddTaskAsync(WorkTask task, CancellationToken ct) =>
        await db.Tasks.AddAsync(task, ct);

    public async Task AddImportPreferenceAsync(TfsImportPreference preference, CancellationToken ct) =>
        await db.TfsImportPreferences.AddAsync(preference, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
