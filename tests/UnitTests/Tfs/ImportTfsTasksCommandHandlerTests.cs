using FluentValidation;
using HonestTimeTracker.Application.Tfs;
using Microsoft.Extensions.Logging.Abstractions;

namespace HonestTimeTracker.UnitTests.Tfs;

public class ImportTfsTasksCommandHandlerTests
{
    private readonly FakeSyncRepository _repository = new();

    private Task<int> ImportAsync(params ImportTfsTaskItem[] items) =>
        new ImportTfsTasksCommandHandler(_repository, NullLogger<ImportTfsTasksCommandHandler>.Instance)
            .HandleAsync(new ImportTfsTasksCommand(items));

    private static ImportTfsTaskItem Item(int workItemId, int projectId = 5, string tfsProject = "Alpha", int collectionId = 1) =>
        new(collectionId, workItemId, tfsProject, $"  Title {workItemId}  ", 90, projectId, IsOnTodayList: true);

    [Fact]
    public async Task Creates_tasks_linked_to_work_items()
    {
        _repository.AddProject(5);

        var created = await ImportAsync(Item(100));

        Assert.Equal(1, created);
        var task = Assert.Single(_repository.Tasks);
        Assert.Equal("Title 100", task.Title);
        Assert.Equal(100, task.TfsWorkItemId);
        Assert.Equal(1, task.TfsCollectionId);
        Assert.Equal(5, task.ProjectId);
        Assert.Equal(90, task.PlannedMinutes);
        Assert.True(task.IsOnTodayList);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task Remembers_last_project_per_tfs_project()
    {
        _repository.AddProject(5);
        _repository.AddProject(6);
        _repository.Preferences.Add(new() { TfsCollectionId = 1, TfsProjectName = "Alpha", LastProjectId = 5 });

        await ImportAsync(Item(100, projectId: 6, tfsProject: "ALPHA"), Item(101, projectId: 5, tfsProject: "Beta"), Item(102, projectId: 6, tfsProject: "Beta"));

        Assert.Equal(2, _repository.Preferences.Count);
        Assert.Equal(6, _repository.Preferences.Single(p => p.TfsProjectName == "Alpha").LastProjectId);
        Assert.Equal(6, _repository.Preferences.Single(p => p.TfsProjectName == "Beta").LastProjectId);
    }

    [Fact]
    public async Task Skips_work_items_already_linked()
    {
        _repository.AddProject(5);
        _repository.AddTask(10, 100, 1, deleted: true);
        _repository.AddTask(11, 101, null);

        var created = await ImportAsync(Item(100), Item(101), Item(102), Item(102));

        Assert.Equal(1, created);
        Assert.Equal(3, _repository.Tasks.Count);
    }

    [Fact]
    public async Task Rejects_closed_project()
    {
        _repository.AddProject(5, closed: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => ImportAsync(Item(100)));
        Assert.Empty(_repository.Tasks);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public void Validator_requires_project_and_title()
    {
        var validator = new ImportTfsTasksCommandValidator();
        var command = new ImportTfsTasksCommand([new ImportTfsTaskItem(1, 100, "Alpha", " ", -1, 0, false)]);

        var result = validator.Validate(command);

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Project is required.");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Task title is required.");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Planned time cannot be negative.");
    }

    [Fact]
    public void Validator_requires_at_least_one_item()
    {
        var validator = new ImportTfsTasksCommandValidator();

        Assert.Throws<ValidationException>(() => validator.ValidateAndThrow(new ImportTfsTasksCommand([])));
    }
}
