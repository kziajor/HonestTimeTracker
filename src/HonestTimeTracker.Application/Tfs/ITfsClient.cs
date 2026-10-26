namespace HonestTimeTracker.Application.Tfs;

public record TfsConnection(string Url, string Pat);

public record TfsWorkItem(int Id, string? Title, string? WorkItemType, string? State, string? TeamProject, int? ParentId);

public record TfsWorkItemTypeState(string Name, string? Category);

public interface ITfsClient
{
    /// Returns the display name of the user the PAT belongs to.
    Task<string> GetAuthenticatedUserAsync(TfsConnection connection, CancellationToken ct);

    Task<IReadOnlyList<int>> QueryWorkItemIdsAsync(TfsConnection connection, string wiql, CancellationToken ct);

    /// Missing or inaccessible work items are omitted from the result.
    Task<IReadOnlyList<TfsWorkItem>> GetWorkItemsAsync(TfsConnection connection, IReadOnlyCollection<int> ids, bool includeRelations, CancellationToken ct);

    Task<IReadOnlyList<TfsWorkItemTypeState>> GetWorkItemTypeStatesAsync(TfsConnection connection, string teamProject, string workItemType, CancellationToken ct);
}
