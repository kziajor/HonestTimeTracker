using HonestTimeTracker.Domain;
using Microsoft.Extensions.Logging;

namespace HonestTimeTracker.Application.Tfs;

/// Closes tasks whose work items are finished in TFS and finds active work items assigned to the PAT owner
/// that are not yet tracked in the application.
public record SyncWithTfsCommand(IProgress<string>? Progress = null) : ICommand<TfsSyncResult>;

public class SyncWithTfsCommandHandler(
    ITfsSyncRepository repository,
    ITfsClient client,
    ISecretProtector secretProtector,
    ILogger<SyncWithTfsCommandHandler> logger) : ICommandHandler<SyncWithTfsCommand, TfsSyncResult>
{
    public const string ActiveWorkItemsWiql =
        "SELECT [System.Id] FROM WorkItems " +
        "WHERE [System.AssignedTo] = @Me " +
        "AND [System.WorkItemType] IN ('Task', 'Bug') " +
        "AND [System.State] NOT IN ('Closed', 'Done', 'Removed') " +
        "ORDER BY [System.Id]";

    private sealed class CollectionContext(TfsCollection collection, TfsConnection connection, TfsStateCategoryResolver resolver)
    {
        public TfsCollection Collection { get; } = collection;
        public TfsConnection Connection { get; } = connection;
        public TfsStateCategoryResolver Resolver { get; } = resolver;
        public bool Failed { get; set; }
        public string Name => Collection.Name;
    }

    private readonly List<TfsClosedTask> _closed = [];
    private readonly List<TfsSkippedTask> _skipped = [];
    private readonly List<TfsCollectionError> _errors = [];
    private int? _activeTimerTaskId;

    public async Task<TfsSyncResult> HandleAsync(SyncWithTfsCommand command, CancellationToken ct = default)
    {
        _closed.Clear();
        _skipped.Clear();
        _errors.Clear();
        _activeTimerTaskId = null;

        var collections = await repository.GetCollectionsAsync(ct);
        var enabled = collections.Where(c => c.Enabled).ToList();
        if (enabled.Count == 0)
            return TfsSyncResult.NoCollections;

        logger.LogInformation("TFS sync started for {Count} collection(s): {Collections}",
            enabled.Count, string.Join(", ", enabled.Select(c => c.Name)));

        var contexts = CreateContexts(enabled);

        await CloseFinishedTasksAsync(contexts, collections, command.Progress, ct);
        await repository.SaveChangesAsync(ct);

        var candidates = await FindImportCandidatesAsync(contexts, command.Progress, ct);

        logger.LogInformation(
            "TFS sync finished: {Closed} task(s) closed, {Skipped} skipped, {Candidates} import candidate(s), {Errors} collection error(s)",
            _closed.Count, _skipped.Count, candidates.Count, _errors.Count);

        return new TfsSyncResult(false, _closed, _skipped, _errors, candidates);
    }

    private List<CollectionContext> CreateContexts(List<TfsCollection> enabled)
    {
        var contexts = new List<CollectionContext>();
        foreach (var collection in enabled)
        {
            if (string.IsNullOrEmpty(collection.EncryptedPat))
            {
                AddError(collection.Name, "Personal access token is not set — update it in Settings.");
                continue;
            }

            string pat;
            try
            {
                pat = secretProtector.Unprotect(collection.EncryptedPat);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot decrypt the PAT of collection {Collection}", collection.Name);
                AddError(collection.Name, "Stored token cannot be decrypted (database copied from another Windows account?) — update it in Settings.");
                continue;
            }

            var connection = new TfsConnection(collection.Url, pat);
            var resolver = new TfsStateCategoryResolver(client, connection, collection.Name, logger);
            contexts.Add(new CollectionContext(collection, connection, resolver));
        }
        return contexts;
    }

    private async Task CloseFinishedTasksAsync(
        List<CollectionContext> contexts, List<TfsCollection> collections, IProgress<string>? progress, CancellationToken ct)
    {
        var tasks = await repository.GetOpenLinkedTasksAsync(ct);
        if (tasks.Count == 0) return;

        _activeTimerTaskId = await repository.GetActiveTimerTaskIdAsync(ct);

        var knownCollectionIds = collections.Select(c => c.Id).ToHashSet();
        foreach (var task in tasks.Where(t => t.TfsCollectionId is { } id && !knownCollectionIds.Contains(id)))
            Skip(task, "TFS collection removed");

        var unassigned = tasks.Where(t => t.TfsCollectionId is null).ToList();
        var unassignedMatches = unassigned.ToDictionary(t => t.Id, _ => new List<(CollectionContext Context, TfsWorkItem WorkItem)>());

        foreach (var context in contexts)
        {
            var own = tasks.Where(t => t.TfsCollectionId == context.Collection.Id).ToList();
            var ids = own.Concat(unassigned).Select(t => t.TfsWorkItemId!.Value).Distinct().ToList();
            if (ids.Count == 0) continue;

            progress?.Report($"Checking task status in {context.Name}…");
            await RunForCollectionAsync(context, async () =>
            {
                var workItems = (await client.GetWorkItemsAsync(context.Connection, ids, false, ct)).ToDictionary(w => w.Id);

                foreach (var task in own)
                {
                    if (workItems.TryGetValue(task.TfsWorkItemId!.Value, out var workItem))
                    {
                        await CloseIfFinishedAsync(context, task, workItem, ct);
                    }
                    else
                    {
                        logger.LogWarning("Work item {WorkItemId} of task {TaskId} not found in collection {Collection}",
                            task.TfsWorkItemId, task.Id, context.Name);
                        Skip(task, $"Work item not found in {context.Name}");
                    }
                }

                foreach (var task in unassigned)
                {
                    if (workItems.TryGetValue(task.TfsWorkItemId!.Value, out var workItem))
                        unassignedMatches[task.Id].Add((context, workItem));
                }
            });
        }

        await ResolveUnassignedTasksAsync(contexts, unassigned, unassignedMatches, ct);
    }

    private async Task ResolveUnassignedTasksAsync(
        List<CollectionContext> contexts,
        List<WorkTask> unassigned,
        Dictionary<int, List<(CollectionContext Context, TfsWorkItem WorkItem)>> matches,
        CancellationToken ct)
    {
        // Self-healing is only safe when every enabled collection answered.
        var allCollectionsResponded = _errors.Count == 0 && contexts.TrueForAll(c => !c.Failed);

        foreach (var task in unassigned)
        {
            var found = matches[task.Id];
            if (!allCollectionsResponded)
            {
                Skip(task, "Collection unknown and not all TFS collections responded");
                continue;
            }

            switch (found.Count)
            {
                case 0:
                    logger.LogWarning("Work item {WorkItemId} of task {TaskId} not found in any TFS collection", task.TfsWorkItemId, task.Id);
                    Skip(task, "Work item not found in any TFS collection");
                    break;

                case 1:
                    var (context, workItem) = found[0];
                    logger.LogInformation("Task {TaskId} assigned to collection {Collection} (work item {WorkItemId})",
                        task.Id, context.Name, task.TfsWorkItemId);
                    task.TfsCollectionId = context.Collection.Id;
                    await RunForCollectionAsync(context, () => CloseIfFinishedAsync(context, task, workItem, ct));
                    break;

                default:
                    var names = string.Join(", ", found.Select(f => f.Context.Name));
                    logger.LogWarning("Work item {WorkItemId} of task {TaskId} exists in multiple collections: {Collections}",
                        task.TfsWorkItemId, task.Id, names);
                    Skip(task, $"Work item ID exists in multiple collections ({names}) — set the collection on the task");
                    break;
            }
        }
    }

    private async Task CloseIfFinishedAsync(CollectionContext context, WorkTask task, TfsWorkItem workItem, CancellationToken ct)
    {
        var category = await context.Resolver.ResolveAsync(workItem, ct);
        if (category is null)
        {
            Skip(task, $"Unrecognized work item state '{workItem.State}' — see log");
            return;
        }

        if (!TfsStateCategories.IsFinished(category)) return;

        if (task.Id == _activeTimerTaskId)
        {
            Skip(task, $"Work item is {workItem.State}, but the timer is running — not closed");
            return;
        }

        task.Closed = true;
        task.ClosedAt = DateTime.UtcNow;
        _closed.Add(new TfsClosedTask(task.Id, task.Title, workItem.Id, context.Name, workItem.State ?? category));
        logger.LogInformation("Task {TaskId} closed — work item {WorkItemId} is {State} in {Collection}",
            task.Id, workItem.Id, workItem.State, context.Name);
    }

    private async Task<List<TfsImportCandidate>> FindImportCandidatesAsync(
        List<CollectionContext> contexts, IProgress<string>? progress, CancellationToken ct)
    {
        var links = await repository.GetAllTaskLinksAsync(ct);
        var linked = links
            .Where(l => l.TfsCollectionId is not null)
            .Select(l => (l.TfsCollectionId!.Value, l.TfsWorkItemId))
            .ToHashSet();
        var linkedWithoutCollection = links
            .Where(l => l.TfsCollectionId is null)
            .Select(l => l.TfsWorkItemId)
            .ToHashSet();

        var openProjectIds = (await repository.GetOpenProjectsAsync(ct)).Select(p => p.Id).ToHashSet();
        var preferences = await repository.GetImportPreferencesAsync(ct);

        var candidates = new List<TfsImportCandidate>();
        foreach (var context in contexts.Where(c => !c.Failed))
        {
            progress?.Report($"Searching work items in {context.Name}…");
            await RunForCollectionAsync(context, async () =>
            {
                var ids = await client.QueryWorkItemIdsAsync(context.Connection, ActiveWorkItemsWiql, ct);
                var newIds = ids
                    .Where(id => !linked.Contains((context.Collection.Id, id)) && !linkedWithoutCollection.Contains(id))
                    .ToList();
                if (newIds.Count == 0) return;

                var workItems = await client.GetWorkItemsAsync(context.Connection, newIds, true, ct);
                var active = new List<TfsWorkItem>();
                foreach (var workItem in workItems)
                {
                    // Unresolvable states are logged by the resolver and treated as active.
                    var category = await context.Resolver.ResolveAsync(workItem, ct);
                    if (!TfsStateCategories.IsFinished(category))
                        active.Add(workItem);
                }

                var parentTitles = await GetParentTitlesAsync(context, active, ct);

                foreach (var workItem in active.OrderBy(w => w.Id))
                {
                    var parentTitle = workItem.ParentId is { } parentId ? parentTitles.GetValueOrDefault(parentId) : null;
                    var title = TfsTaskTitleComposer.Compose(parentTitle, workItem.Title ?? $"Work item {workItem.Id}");
                    var teamProject = workItem.TeamProject ?? string.Empty;

                    var lastProjectId = preferences
                        .FirstOrDefault(p => p.TfsCollectionId == context.Collection.Id
                            && string.Equals(p.TfsProjectName, teamProject, StringComparison.OrdinalIgnoreCase))
                        ?.LastProjectId;
                    int? preselected = lastProjectId is { } projectId && openProjectIds.Contains(projectId) ? projectId : null;

                    candidates.Add(new TfsImportCandidate(context.Collection.Id, context.Name, workItem.Id,
                        workItem.WorkItemType ?? string.Empty, teamProject, title, preselected));
                }
            });
        }
        return candidates;
    }

    private async Task<Dictionary<int, string?>> GetParentTitlesAsync(CollectionContext context, List<TfsWorkItem> workItems, CancellationToken ct)
    {
        var parentIds = workItems.Select(w => w.ParentId).OfType<int>().Distinct().ToList();
        if (parentIds.Count == 0) return [];

        var parents = (await client.GetWorkItemsAsync(context.Connection, parentIds, false, ct)).ToDictionary(p => p.Id, p => p.Title);
        foreach (var missing in parentIds.Where(id => !parents.ContainsKey(id)))
            logger.LogDebug("Parent work item {ParentId} not accessible in collection {Collection}", missing, context.Name);
        return parents;
    }

    private async Task RunForCollectionAsync(CollectionContext context, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (TfsException ex)
        {
            context.Failed = true;
            logger.LogWarning(ex, "TFS sync failed for collection {Collection} ({Kind})", context.Name, ex.Kind);
            AddError(context.Name, Describe(ex));
        }
    }

    private void AddError(string collectionName, string message)
    {
        if (!_errors.Exists(e => e.CollectionName == collectionName && e.Message == message))
            _errors.Add(new TfsCollectionError(collectionName, message));
    }

    private void Skip(WorkTask task, string reason) =>
        _skipped.Add(new TfsSkippedTask(task.Id, task.Title, task.TfsWorkItemId ?? 0, reason));

    private static string Describe(TfsException ex) => ex.Kind switch
    {
        TfsErrorKind.Unauthorized => "PAT invalid or expired — update it in Settings.",
        TfsErrorKind.Unreachable => "Server unreachable.",
        _ => ex.Message
    };
}
