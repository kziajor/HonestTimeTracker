using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Projects;
using HonestTimeTracker.Application.Records;
using HonestTimeTracker.Application.Settings;
using HonestTimeTracker.Application.Tasks;
using HonestTimeTracker.Desktop.Common;
using HonestTimeTracker.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace HonestTimeTracker.Desktop.Features.Tasks;

public class TasksViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITimerStateService _timerStateService;
    private readonly ITimerStopService _timerStopService;
    private bool _showClosed = false;
    private string _titleFilter = string.Empty;
    private string _tfsIdFilter = string.Empty;

    public ObservableCollection<TaskDto> Tasks { get; } = [];
    public ICollectionView TasksView { get; }

    public bool ShowClosed
    {
        get => _showClosed;
        set { if (Set(ref _showClosed, value)) _ = LoadAsync(); }
    }

    public string TitleFilter
    {
        get => _titleFilter;
        set { if (Set(ref _titleFilter, value)) TasksView.Refresh(); }
    }

    public string TfsIdFilter
    {
        get => _tfsIdFilter;
        set { if (Set(ref _tfsIdFilter, value)) TasksView.Refresh(); }
    }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleClosedCommand { get; }
    public ICommand ToggleTodayListCommand { get; }
    public ICommand StartTimerCommand { get; }
    public ICommand StopTimerCommand { get; }

    public TasksViewModel(IServiceScopeFactory scopeFactory, ITimerStateService timerStateService, ITimerStopService timerStopService)
    {
        _scopeFactory = scopeFactory;
        _timerStateService = timerStateService;
        _timerStopService = timerStopService;

        TasksView = CollectionViewSource.GetDefaultView(Tasks);
        TasksView.Filter = FilterTask;

        AddCommand = new AsyncRelayCommand(_ => AddAsync());
        EditCommand = new AsyncRelayCommand(p => EditAsync((TaskDto)p!), p => p is TaskDto);
        DeleteCommand = new AsyncRelayCommand(p => DeleteAsync((TaskDto)p!), p => p is TaskDto);
        ToggleClosedCommand = new AsyncRelayCommand(p => ToggleClosedAsync((TaskDto)p!), p => p is TaskDto);
        ToggleTodayListCommand = new AsyncRelayCommand(p => ToggleTodayListAsync((TaskDto)p!), p => p is TaskDto);
        StartTimerCommand = new AsyncRelayCommand(p => StartTimerAsync((TaskDto)p!), p => p is TaskDto);
        StopTimerCommand = new AsyncRelayCommand(p => StopTimerAsync((TaskDto)p!), p => p is TaskDto);
    }

    private bool FilterTask(object obj)
    {
        if (obj is not TaskDto task) return false;
        if (!string.IsNullOrWhiteSpace(_titleFilter) &&
            !task.Title.Contains(_titleFilter, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrWhiteSpace(_tfsIdFilter) &&
            int.TryParse(_tfsIdFilter, out var id) &&
            task.TfsWorkItemId != id)
            return false;
        return true;
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

    private async Task<double?> GetDefaultTaskPlannedHoursAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetSettingsQuery, SettingsDto>>();
        var settings = await handler.HandleAsync(new GetSettingsQuery());
        return settings.DefaultTaskPlannedHours;
    }

    private async Task AddAsync()
    {
        var projects = await GetProjectsAsync();
        var defaultHours = await GetDefaultTaskPlannedHoursAsync();
        var dialog = new TaskDialog(projects, defaultPlannedHours: defaultHours) { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateTaskCommand, int>>();
        try
        {
            var newId = await handler.HandleAsync(new CreateTaskCommand(dialog.TaskTitle, dialog.PlannedMinutes, dialog.ProjectId, dialog.TfsWorkItemId));
            if (dialog.IsOnTodayList)
            {
                using var todayScope = _scopeFactory.CreateScope();
                var todayHandler = todayScope.ServiceProvider.GetRequiredService<ICommandHandler<SetTodayListCommand, Unit>>();
                await todayHandler.HandleAsync(new SetTodayListCommand(newId, true));
            }
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
        var dialog = new TaskDialog(projects, task.Title, task.PlannedMinutes, task.ProjectId, existingTfsWorkItemId: task.TfsWorkItemId, existingIsOnTodayList: task.IsOnTodayList)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateTaskCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new UpdateTaskCommand(task.Id, dialog.TaskTitle, dialog.PlannedMinutes, dialog.ProjectId, dialog.TfsWorkItemId));
            if (dialog.IsOnTodayList != task.IsOnTodayList)
            {
                using var todayScope = _scopeFactory.CreateScope();
                var todayHandler = todayScope.ServiceProvider.GetRequiredService<ICommandHandler<SetTodayListCommand, Unit>>();
                await todayHandler.HandleAsync(new SetTodayListCommand(task.Id, dialog.IsOnTodayList));
            }
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

    private async Task ToggleTodayListAsync(TaskDto task)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SetTodayListCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new SetTodayListCommand(task.Id, !task.IsOnTodayList));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task StartTimerAsync(TaskDto task)
    {
        var activeTask = Tasks.FirstOrDefault(t => t.HasActiveTimer);
        if (activeTask is not null && activeTask.Id != task.Id)
        {
            var confirm = MessageBox.Show(
                $"Timer is running for \"{activeTask.Title}\".\nStop it and start tracking \"{task.Title}\"?",
                "Timer already running",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            using var stopScope = _scopeFactory.CreateScope();
            var stopHandler = stopScope.ServiceProvider.GetRequiredService<ICommandHandler<StopTimerCommand, Unit>>();
            try
            {
                await stopHandler.HandleAsync(new StopTimerCommand());
                _timerStateService.NotifyTimerStopped();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<StartTimerCommand, int>>();
        try
        {
            await handler.HandleAsync(new StartTimerCommand(task.Id));
            _timerStateService.NotifyTimerStarted(task.Id, task.Title, task.SpentMinutes, task.PlannedMinutes, DateTime.Now, task.TfsWorkItemId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task StopTimerAsync(TaskDto task)
    {
        if (await _timerStopService.SafeStopAsync())
            await LoadAsync();
    }
}
