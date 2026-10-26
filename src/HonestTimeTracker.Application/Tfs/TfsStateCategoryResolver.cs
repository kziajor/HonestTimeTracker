using Microsoft.Extensions.Logging;

namespace HonestTimeTracker.Application.Tfs;

/// Resolves work item states to state categories, caching the state map per team project and work item type.
public class TfsStateCategoryResolver(ITfsClient client, TfsConnection connection, string collectionName, ILogger logger)
{
    private readonly Dictionary<(string Project, string Type), Dictionary<string, string?>> _cache = [];

    /// Returns the state category, or null when the state cannot be resolved.
    public async Task<string?> ResolveAsync(TfsWorkItem workItem, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(workItem.TeamProject) || string.IsNullOrEmpty(workItem.WorkItemType) || string.IsNullOrEmpty(workItem.State))
        {
            logger.LogWarning(
                "Work item {WorkItemId} in collection {Collection} is missing required fields (project: {Project}, type: {Type}, state: {State})",
                workItem.Id, collectionName, workItem.TeamProject, workItem.WorkItemType, workItem.State);
            return null;
        }

        var key = (workItem.TeamProject, workItem.WorkItemType);
        if (!_cache.TryGetValue(key, out var states))
        {
            var list = await client.GetWorkItemTypeStatesAsync(connection, workItem.TeamProject, workItem.WorkItemType, ct);
            states = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var state in list)
                states[state.Name] = state.Category;
            _cache[key] = states;
        }

        if (!states.TryGetValue(workItem.State, out var category))
        {
            logger.LogWarning(
                "Unrecognized state '{State}' of work item {WorkItemId} ({Type}, project {Project}, collection {Collection}). Known states: {KnownStates}",
                workItem.State, workItem.Id, workItem.WorkItemType, workItem.TeamProject, collectionName, string.Join(", ", states.Keys));
            return null;
        }

        if (!TfsStateCategories.IsKnown(category))
        {
            logger.LogWarning(
                "State '{State}' of work item {WorkItemId} ({Type}, project {Project}, collection {Collection}) has unrecognized category '{Category}'",
                workItem.State, workItem.Id, workItem.WorkItemType, workItem.TeamProject, collectionName, category);
            return null;
        }

        return category;
    }
}
