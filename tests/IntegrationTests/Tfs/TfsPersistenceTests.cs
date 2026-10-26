using HonestTimeTracker.Application.Tfs;
using HonestTimeTracker.Domain;
using HonestTimeTracker.Infrastructure.Repositories;
using HonestTimeTracker.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HonestTimeTracker.IntegrationTests.Tfs;

public sealed class TfsPersistenceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly int _collectionId;
    private readonly int _projectId;

    public TfsPersistenceTests()
    {
        using var db = _database.CreateContext();
        var collection = new TfsCollection { Name = "Main", Url = "https://tfs.test/tfs/Main", EncryptedPat = "x" };
        var project = new Project { Name = "Alpha - implementation" };
        db.AddRange(collection, project);
        db.SaveChanges();
        _collectionId = collection.Id;
        _projectId = project.Id;
    }

    public void Dispose() => _database.Dispose();

    private void SeedTasks()
    {
        using var db = _database.CreateContext();
        db.Tasks.AddRange(
            new WorkTask { Title = "open", TfsWorkItemId = 1, TfsCollectionId = _collectionId, ProjectId = _projectId },
            new WorkTask { Title = "closed", TfsWorkItemId = 2, TfsCollectionId = _collectionId, Closed = true },
            new WorkTask { Title = "deleted", TfsWorkItemId = 3, IsDeleted = true },
            new WorkTask { Title = "manual" });
        db.SaveChanges();
    }

    [Fact]
    public async Task Task_links_include_closed_and_deleted_tasks()
    {
        SeedTasks();
        await using var db = _database.CreateContext();

        var links = await new TfsSyncRepository(db).GetAllTaskLinksAsync(CancellationToken.None);

        Assert.Equal(
            [new TfsTaskLink(_collectionId, 1), new TfsTaskLink(_collectionId, 2), new TfsTaskLink(null, 3)],
            links.OrderBy(l => l.TfsWorkItemId));
    }

    [Fact]
    public async Task Open_linked_tasks_exclude_closed_deleted_and_unlinked()
    {
        SeedTasks();
        await using var db = _database.CreateContext();

        var tasks = await new TfsSyncRepository(db).GetOpenLinkedTasksAsync(CancellationToken.None);

        Assert.Equal("open", Assert.Single(tasks).Title);
    }

    [Fact]
    public async Task Active_timer_task_is_the_unfinished_record()
    {
        SeedTasks();
        await using (var seed = _database.CreateContext())
        {
            var task = seed.Tasks.Single(t => t.Title == "open");
            seed.Records.AddRange(
                new WorkRecord { TaskId = task.Id, StartedAt = DateTime.Today, FinishedAt = DateTime.Today.AddHours(1) },
                new WorkRecord { TaskId = task.Id, StartedAt = DateTime.Today.AddHours(2) });
            await seed.SaveChangesAsync();
        }
        await using var db = _database.CreateContext();

        var activeTaskId = await new TfsSyncRepository(db).GetActiveTimerTaskIdAsync(CancellationToken.None);

        Assert.Equal(db.Tasks.Single(t => t.Title == "open").Id, activeTaskId);
    }

    [Fact]
    public async Task Import_creates_tasks_and_keeps_one_preference_per_tfs_project()
    {
        var item = new ImportTfsTaskItem(_collectionId, 500, "Alpha", "Story - Task", 120, _projectId, true);

        await using (var db = _database.CreateContext())
        {
            var handler = new ImportTfsTasksCommandHandler(new TfsSyncRepository(db), NullLogger<ImportTfsTasksCommandHandler>.Instance);
            Assert.Equal(1, await handler.HandleAsync(new ImportTfsTasksCommand([item])));
        }

        await using (var db = _database.CreateContext())
        {
            var handler = new ImportTfsTasksCommandHandler(new TfsSyncRepository(db), NullLogger<ImportTfsTasksCommandHandler>.Instance);
            var again = await handler.HandleAsync(new ImportTfsTasksCommand([item, item with { TfsWorkItemId = 501, TfsProjectName = "ALPHA" }]));
            Assert.Equal(1, again);
        }

        await using var verify = _database.CreateContext();
        var tasks = await verify.Tasks.OrderBy(t => t.TfsWorkItemId).ToListAsync();
        Assert.Equal([500, 501], tasks.Select(t => t.TfsWorkItemId));
        Assert.All(tasks, t => Assert.Equal(_collectionId, t.TfsCollectionId));
        Assert.True(tasks[0].IsOnTodayList);
        var preference = Assert.Single(await verify.TfsImportPreferences.ToListAsync());
        Assert.Equal(_projectId, preference.LastProjectId);
    }

    [Fact]
    public async Task Preference_project_name_is_unique_per_collection_ignoring_case()
    {
        await using var db = _database.CreateContext();
        db.TfsImportPreferences.AddRange(
            new TfsImportPreference { TfsCollectionId = _collectionId, TfsProjectName = "Alpha", LastProjectId = _projectId },
            new TfsImportPreference { TfsCollectionId = _collectionId, TfsProjectName = "alpha", LastProjectId = _projectId });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Collection_name_and_url_checks_ignore_case_and_deleted_collections()
    {
        await using (var seed = _database.CreateContext())
        {
            seed.TfsCollections.Add(new TfsCollection { Name = "Old", Url = "https://tfs.test/tfs/Old", IsDeleted = true });
            await seed.SaveChangesAsync();
        }
        await using var db = _database.CreateContext();
        var repository = new TfsCollectionRepository(db);

        Assert.True(await repository.NameExistsAsync("MAIN", null, CancellationToken.None));
        Assert.False(await repository.NameExistsAsync("main", _collectionId, CancellationToken.None));
        Assert.True(await repository.UrlExistsAsync("https://TFS.test/tfs/main", null, CancellationToken.None));
        Assert.False(await repository.NameExistsAsync("Old", null, CancellationToken.None));
    }

    [Fact]
    public async Task Deleting_collection_is_soft_and_unlinks_nothing()
    {
        SeedTasks();
        await using (var db = _database.CreateContext())
        {
            var handler = new DeleteTfsCollectionCommandHandler(new TfsCollectionRepository(db));
            await handler.HandleAsync(new DeleteTfsCollectionCommand(_collectionId));
        }

        await using var verify = _database.CreateContext();
        Assert.Empty(await new TfsSyncRepository(verify).GetCollectionsAsync(CancellationToken.None));
        Assert.True((await verify.TfsCollections.IgnoreQueryFilters().SingleAsync(c => c.Id == _collectionId)).IsDeleted);
        Assert.Equal(_collectionId, (await verify.Tasks.SingleAsync(t => t.Title == "open")).TfsCollectionId);
    }
}
