using HonestTimeTracker.Application.Tfs;
using HonestTimeTracker.Domain;

namespace HonestTimeTracker.UnitTests.Tfs;

internal sealed class FakeTfsCollection
{
    public Dictionary<int, TfsWorkItem> WorkItems { get; } = [];
    public List<int> AssignedActiveIds { get; } = [];
    public TfsException? Failure { get; set; }

    public Dictionary<string, string?> States { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["New"] = TfsStateCategories.Proposed,
        ["Active"] = TfsStateCategories.InProgress,
        ["Resolved"] = TfsStateCategories.Resolved,
        ["Closed"] = TfsStateCategories.Completed,
        ["Done"] = TfsStateCategories.Completed,
        ["Removed"] = TfsStateCategories.Removed
    };

    public FakeTfsCollection With(int id, string state, string? title = null, int? parentId = null, string type = "Task", string project = "Alpha")
    {
        WorkItems[id] = new TfsWorkItem(id, title ?? $"Item {id}", type, state, project, parentId);
        return this;
    }

    public FakeTfsCollection Assigned(params int[] ids)
    {
        AssignedActiveIds.AddRange(ids);
        return this;
    }
}

internal sealed class FakeTfsClient : ITfsClient
{
    public Dictionary<string, FakeTfsCollection> Collections { get; } = [];
    public List<string> RequestedUrls { get; } = [];

    public FakeTfsCollection Add(string url)
    {
        var collection = new FakeTfsCollection();
        Collections[url] = collection;
        return collection;
    }

    private FakeTfsCollection Get(TfsConnection connection)
    {
        RequestedUrls.Add(connection.Url);
        var collection = Collections[connection.Url];
        if (collection.Failure is not null) throw collection.Failure;
        return collection;
    }

    public Task<string> GetAuthenticatedUserAsync(TfsConnection connection, CancellationToken ct)
    {
        Get(connection);
        return Task.FromResult("Test User");
    }

    public Task<IReadOnlyList<int>> QueryWorkItemIdsAsync(TfsConnection connection, string wiql, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<int>>(Get(connection).AssignedActiveIds.ToList());

    public Task<IReadOnlyList<TfsWorkItem>> GetWorkItemsAsync(TfsConnection connection, IReadOnlyCollection<int> ids, bool includeRelations, CancellationToken ct)
    {
        var collection = Get(connection);
        IReadOnlyList<TfsWorkItem> result = ids.Where(collection.WorkItems.ContainsKey).Select(id => collection.WorkItems[id]).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<TfsWorkItemTypeState>> GetWorkItemTypeStatesAsync(TfsConnection connection, string teamProject, string workItemType, CancellationToken ct)
    {
        IReadOnlyList<TfsWorkItemTypeState> result = Get(connection).States.Select(s => new TfsWorkItemTypeState(s.Key, s.Value)).ToList();
        return Task.FromResult(result);
    }
}

internal sealed class FakeSyncRepository : ITfsSyncRepository
{
    public List<TfsCollection> Collections { get; } = [];
    public List<WorkTask> Tasks { get; } = [];
    public List<Project> Projects { get; } = [];
    public List<TfsImportPreference> Preferences { get; } = [];
    public int? ActiveTimerTaskId { get; set; }
    public int SaveCount { get; private set; }

    public TfsCollection AddCollection(int id, string name, bool enabled = true, bool deleted = false)
    {
        var collection = new TfsCollection { Id = id, Name = name, Url = Url(id), EncryptedPat = "pat", Enabled = enabled, IsDeleted = deleted };
        Collections.Add(collection);
        return collection;
    }

    public WorkTask AddTask(int id, int? workItemId, int? collectionId, bool closed = false, bool deleted = false)
    {
        var task = new WorkTask { Id = id, Title = $"Task {id}", TfsWorkItemId = workItemId, TfsCollectionId = collectionId, Closed = closed, IsDeleted = deleted };
        Tasks.Add(task);
        return task;
    }

    public Project AddProject(int id, bool closed = false)
    {
        var project = new Project { Id = id, Name = $"Project {id}", Closed = closed };
        Projects.Add(project);
        return project;
    }

    public static string Url(int collectionId) => $"https://tfs.test/tfs/c{collectionId}";

    public Task<List<TfsCollection>> GetCollectionsAsync(CancellationToken ct) =>
        Task.FromResult(Collections.Where(c => !c.IsDeleted).ToList());

    public Task<List<WorkTask>> GetOpenLinkedTasksAsync(CancellationToken ct) =>
        Task.FromResult(Tasks.Where(t => !t.IsDeleted && !t.Closed && t.TfsWorkItemId != null).ToList());

    public Task<List<TfsTaskLink>> GetAllTaskLinksAsync(CancellationToken ct) =>
        Task.FromResult(Tasks.Where(t => t.TfsWorkItemId != null).Select(t => new TfsTaskLink(t.TfsCollectionId, t.TfsWorkItemId!.Value)).ToList());

    public Task<int?> GetActiveTimerTaskIdAsync(CancellationToken ct) => Task.FromResult(ActiveTimerTaskId);

    public Task<List<Project>> GetOpenProjectsAsync(CancellationToken ct) =>
        Task.FromResult(Projects.Where(p => !p.Closed && !p.IsDeleted).ToList());

    public Task<List<TfsImportPreference>> GetImportPreferencesAsync(CancellationToken ct) =>
        Task.FromResult(Preferences.ToList());

    public Task AddTaskAsync(WorkTask task, CancellationToken ct)
    {
        Tasks.Add(task);
        return Task.CompletedTask;
    }

    public Task AddImportPreferenceAsync(TfsImportPreference preference, CancellationToken ct)
    {
        Preferences.Add(preference);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class PlainSecretProtector : ISecretProtector
{
    public string Protect(string plainText) => plainText;
    public string Unprotect(string protectedText) => protectedText;
}
