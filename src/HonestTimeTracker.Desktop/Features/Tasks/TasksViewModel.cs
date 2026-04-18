using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Projects;
using HonestTimeTracker.Application.Tasks;
using HonestTimeTracker.Desktop.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HonestTimeTracker.Desktop.Features.Tasks;

public class TasksViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private bool _showClosed = false;

    public ObservableCollection<TaskDto> Tasks { get; } = [];

    public bool ShowClosed
    {
        get => _showClosed;
        set { if (Set(ref _showClosed, value)) _ = LoadAsync(); }
    }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleClosedCommand { get; }

    public TasksViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        AddCommand = new AsyncRelayCommand(_ => AddAsync());
        EditCommand = new AsyncRelayCommand(p => EditAsync((TaskDto)p!), p => p is TaskDto);
        DeleteCommand = new AsyncRelayCommand(p => DeleteAsync((TaskDto)p!), p => p is TaskDto);
        ToggleClosedCommand = new AsyncRelayCommand(p => ToggleClosedAsync((TaskDto)p!), p => p is TaskDto);
    }

    public async Task LoadAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetTasksQuery, List<TaskDto>>>();
        var tasks = await handler.HandleAsync(new GetTasksQuery(ShowClosed));
        Tasks.Clear();
        foreach (var t in tasks) Tasks.Add(t);
    }

    private async Task<List<ProjectDto>> GetProjectsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetProjectsQuery, List<ProjectDto>>>();
        return await handler.HandleAsync(new GetProjectsQuery(ShowClosed: false));
    }

    private async Task AddAsync()
    {
        var projects = await GetProjectsAsync();
        var dialog = new TaskDialog(projects) { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateTaskCommand, int>>();
        try
        {
            await handler.HandleAsync(new CreateTaskCommand(dialog.TaskTitle, dialog.PlannedMinutes, dialog.ProjectId));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task EditAsync(TaskDto task)
    {
        var projects = await GetProjectsAsync();
        var dialog = new TaskDialog(projects, task.Title, task.PlannedMinutes, task.ProjectId)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateTaskCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new UpdateTaskCommand(task.Id, dialog.TaskTitle, dialog.PlannedMinutes, dialog.ProjectId));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task DeleteAsync(TaskDto task)
    {
        var confirm = MessageBox.Show(
            $"Are you sure you want to delete task \"{task.Title}\"?\nThis action cannot be undone.",
            "Delete task",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeleteTaskCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new DeleteTaskCommand(task.Id));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task ToggleClosedAsync(TaskDto task)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ToggleTaskClosedCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new ToggleTaskClosedCommand(task.Id));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
