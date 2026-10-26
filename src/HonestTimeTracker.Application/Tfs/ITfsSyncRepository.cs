using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Tfs;

public record TfsTaskLink(int? TfsCollectionId, int TfsWorkItemId);

public interface ITfsSyncRepository
{
    /// All non-deleted collections, including disabled ones.
    Task<List<TfsCollection>> GetCollectionsAsync(CancellationToken ct);

    /// Open tasks with a TFS work item ID (tracked, so changes are saved by SaveChangesAsync).
    Task<List<WorkTask>> GetOpenLinkedTasksAsync(CancellationToken ct);

    /// Work item links of all tasks, including closed and deleted ones.
    Task<List<TfsTaskLink>> GetAllTaskLinksAsync(CancellationToken ct);

    Task<int?> GetActiveTimerTaskIdAsync(CancellationToken ct);
    Task<List<Project>> GetOpenProjectsAsync(CancellationToken ct);
    Task<List<TfsImportPreference>> GetImportPreferencesAsync(CancellationToken ct);
    Task AddTaskAsync(WorkTask task, CancellationToken ct);
    Task AddImportPreferenceAsync(TfsImportPreference preference, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
