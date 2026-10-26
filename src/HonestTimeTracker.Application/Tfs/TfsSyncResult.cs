namespace HonestTimeTracker.Application.Tfs;

public record TfsClosedTask(int TaskId, string Title, int WorkItemId, string CollectionName, string State);

public record TfsSkippedTask(int TaskId, string Title, int WorkItemId, string Reason);

public record TfsCollectionError(string CollectionName, string Message);

public record TfsImportCandidate(
    int CollectionId,
    string CollectionName,
    int WorkItemId,
    string WorkItemType,
    string TeamProject,
    string ProposedTitle,
    int? PreselectedProjectId);

public record TfsSyncResult(
    bool NoCollectionsConfigured,
    IReadOnlyList<TfsClosedTask> ClosedTasks,
    IReadOnlyList<TfsSkippedTask> SkippedTasks,
    IReadOnlyList<TfsCollectionError> Errors,
    IReadOnlyList<TfsImportCandidate> Candidates)
{
    public static TfsSyncResult NoCollections { get; } = new(true, [], [], [], []);
}
