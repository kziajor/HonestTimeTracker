using HonestTimeTracker.Application.Tfs;
using Microsoft.Extensions.Logging.Abstractions;

namespace HonestTimeTracker.UnitTests.Tfs;

public class SyncWithTfsCommandHandlerTests
{
    private readonly FakeSyncRepository _repository = new();
    private readonly FakeTfsClient _client = new();

    private Task<TfsSyncResult> SyncAsync() =>
        new SyncWithTfsCommandHandler(_repository, _client, new PlainSecretProtector(), NullLogger<SyncWithTfsCommandHandler>.Instance)
            .HandleAsync(new SyncWithTfsCommand());

    private FakeTfsCollection AddCollection(int id, string? name = null)
    {
        _repository.AddCollection(id, name ?? $"C{id}");
        return _client.Add(FakeSyncRepository.Url(id));
    }

    [Fact]
    public async Task Returns_no_collections_when_none_is_enabled()
    {
        _repository.AddCollection(1, "Disabled", enabled: false);

        var result = await SyncAsync();

        Assert.True(result.NoCollectionsConfigured);
        Assert.Empty(_client.RequestedUrls);
    }

    [Theory]
    [InlineData("Closed")]
    [InlineData("Done")]
    [InlineData("Removed")]
    public async Task Closes_task_when_work_item_is_completed_or_removed(string state)
    {
        AddCollection(1).With(100, state);
        var task = _repository.AddTask(10, 100, 1);

        var result = await SyncAsync();

        Assert.True(task.Closed);
        Assert.NotNull(task.ClosedAt);
        var closed = Assert.Single(result.ClosedTasks);
        Assert.Equal(state, closed.State);
        Assert.True(_repository.SaveCount > 0);
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Resolved")]
    [InlineData("New")]
    public async Task Does_not_close_task_when_work_item_is_not_finished(string state)
    {
        AddCollection(1).With(100, state);
        var task = _repository.AddTask(10, 100, 1);

        var result = await SyncAsync();

        Assert.False(task.Closed);
        Assert.Empty(result.ClosedTasks);
        Assert.Empty(result.SkippedTasks);
    }

    [Fact]
    public async Task Uses_state_category_for_custom_states()
    {
        var collection = AddCollection(1).With(100, "Shipped");
        collection.States["Shipped"] = TfsStateCategories.Completed;
        var task = _repository.AddTask(10, 100, 1);

        await SyncAsync();

        Assert.True(task.Closed);
    }

    [Fact]
    public async Task Skips_task_with_running_timer()
    {
        AddCollection(1).With(100, "Closed");
        var task = _repository.AddTask(10, 100, 1);
        _repository.ActiveTimerTaskId = 10;

        var result = await SyncAsync();

        Assert.False(task.Closed);
        var skipped = Assert.Single(result.SkippedTasks);
        Assert.Contains("timer is running", skipped.Reason);
    }

    [Fact]
    public async Task Skips_task_with_unrecognized_state()
    {
        AddCollection(1).With(100, "Mystery");
        var task = _repository.AddTask(10, 100, 1);

        var result = await SyncAsync();

        Assert.False(task.Closed);
        Assert.Contains("Mystery", Assert.Single(result.SkippedTasks).Reason);
    }

    [Fact]
    public async Task Skips_task_whose_work_item_no_longer_exists()
    {
        AddCollection(1);
        _repository.AddTask(10, 100, 1);

        var result = await SyncAsync();

        Assert.Contains("not found", Assert.Single(result.SkippedTasks).Reason);
    }

    [Fact]
    public async Task Skips_task_linked_to_removed_collection()
    {
        AddCollection(1);
        _repository.AddCollection(2, "Gone", deleted: true);
        _repository.AddTask(10, 100, 2);

        var result = await SyncAsync();

        Assert.Equal("TFS collection removed", Assert.Single(result.SkippedTasks).Reason);
    }

    [Fact]
    public async Task Does_not_query_disabled_collection()
    {
        AddCollection(1);
        _repository.AddCollection(2, "Disabled", enabled: false);
        _client.Add(FakeSyncRepository.Url(2)).With(100, "Closed");
        var task = _repository.AddTask(10, 100, 2);

        await SyncAsync();

        Assert.False(task.Closed);
        Assert.DoesNotContain(FakeSyncRepository.Url(2), _client.RequestedUrls);
    }

    [Fact]
    public async Task Assigns_collection_to_unassigned_task_found_in_exactly_one_collection()
    {
        AddCollection(1).With(100, "Closed");
        AddCollection(2);
        var task = _repository.AddTask(10, 100, null);

        var result = await SyncAsync();

        Assert.Equal(1, task.TfsCollectionId);
        Assert.True(task.Closed);
        Assert.Single(result.ClosedTasks);
    }

    [Fact]
    public async Task Skips_unassigned_task_found_in_multiple_collections()
    {
        AddCollection(1).With(100, "Closed");
        AddCollection(2).With(100, "Active");
        var task = _repository.AddTask(10, 100, null);

        var result = await SyncAsync();

        Assert.Null(task.TfsCollectionId);
        Assert.False(task.Closed);
        Assert.Contains("multiple collections", Assert.Single(result.SkippedTasks).Reason);
    }

    [Fact]
    public async Task Does_not_self_heal_unassigned_task_when_a_collection_failed()
    {
        AddCollection(1).With(100, "Closed");
        AddCollection(2).Failure = new TfsException(TfsErrorKind.Unreachable, "down");
        var task = _repository.AddTask(10, 100, null);

        var result = await SyncAsync();

        Assert.Null(task.TfsCollectionId);
        Assert.False(task.Closed);
        Assert.Contains("not all TFS collections responded", Assert.Single(result.SkippedTasks).Reason);
    }

    [Fact]
    public async Task Failure_of_one_collection_does_not_stop_others()
    {
        AddCollection(1, "Broken").Failure = new TfsException(TfsErrorKind.Unauthorized, "401");
        AddCollection(2).With(200, "Closed").With(201, "Active").Assigned(201);
        var task = _repository.AddTask(10, 200, 2);

        var result = await SyncAsync();

        Assert.True(task.Closed);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Broken", error.CollectionName);
        Assert.Contains("PAT invalid or expired", error.Message);
        Assert.Equal(201, Assert.Single(result.Candidates).WorkItemId);
    }

    [Fact]
    public async Task Reports_error_when_collection_has_no_token()
    {
        AddCollection(1);
        _repository.Collections[0].EncryptedPat = null;

        var result = await SyncAsync();

        Assert.Contains("token is not set", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public async Task Candidates_exclude_work_items_linked_to_any_task_including_closed_and_deleted()
    {
        AddCollection(1)
            .With(100, "Active").With(101, "Active").With(102, "Active").With(103, "Active").With(104, "Active")
            .Assigned(100, 101, 102, 103, 104);
        _repository.AddTask(10, 100, 1);
        _repository.AddTask(11, 101, 1, closed: true);
        _repository.AddTask(12, 102, 1, deleted: true);
        _repository.AddTask(13, 103, null, closed: true);

        var result = await SyncAsync();

        Assert.Equal([104], result.Candidates.Select(c => c.WorkItemId));
    }

    [Fact]
    public async Task Same_work_item_id_in_other_collection_is_still_a_candidate()
    {
        AddCollection(1);
        AddCollection(2).With(100, "Active").Assigned(100);
        _repository.AddTask(10, 100, 1, closed: true);

        var result = await SyncAsync();

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal(2, candidate.CollectionId);
    }

    [Fact]
    public async Task Candidates_exclude_finished_work_items_with_custom_states()
    {
        var collection = AddCollection(1).With(100, "Shipped").With(101, "Active").Assigned(100, 101);
        collection.States["Shipped"] = TfsStateCategories.Completed;

        var result = await SyncAsync();

        Assert.Equal([101], result.Candidates.Select(c => c.WorkItemId));
    }

    [Fact]
    public async Task Candidate_title_includes_parent_title()
    {
        AddCollection(1)
            .With(50, "Active", "User story", type: "User Story")
            .With(100, "Active", "Implement API", parentId: 50)
            .With(101, "Active", "Orphan")
            .With(102, "Active", "Lost parent", parentId: 999)
            .Assigned(100, 101, 102);

        var result = await SyncAsync();

        var titles = result.Candidates.ToDictionary(c => c.WorkItemId, c => c.ProposedTitle);
        Assert.Equal("User story - Implement API", titles[100]);
        Assert.Equal("Orphan", titles[101]);
        Assert.Equal("Lost parent", titles[102]);
    }

    [Fact]
    public async Task Preselects_last_used_project_for_tfs_project()
    {
        AddCollection(1).With(100, "Active", project: "Alpha").With(101, "Active", project: "Beta").Assigned(100, 101);
        _repository.AddProject(5);
        _repository.Preferences.Add(new() { TfsCollectionId = 1, TfsProjectName = "alpha", LastProjectId = 5 });

        var result = await SyncAsync();

        var byId = result.Candidates.ToDictionary(c => c.WorkItemId);
        Assert.Equal(5, byId[100].PreselectedProjectId);
        Assert.Null(byId[101].PreselectedProjectId);
    }

    [Fact]
    public async Task Does_not_preselect_closed_project()
    {
        AddCollection(1).With(100, "Active").Assigned(100);
        _repository.AddProject(5, closed: true);
        _repository.Preferences.Add(new() { TfsCollectionId = 1, TfsProjectName = "Alpha", LastProjectId = 5 });

        var result = await SyncAsync();

        Assert.Null(Assert.Single(result.Candidates).PreselectedProjectId);
    }

    [Fact]
    public async Task Does_not_search_candidates_in_failed_collection()
    {
        AddCollection(1).Failure = new TfsException(TfsErrorKind.Other, "boom");

        var result = await SyncAsync();

        Assert.Empty(result.Candidates);
        Assert.Equal("boom", Assert.Single(result.Errors).Message);
    }
}
