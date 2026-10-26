using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Projects;
using HonestTimeTracker.Application.Settings;
using HonestTimeTracker.Application.Tasks;
using HonestTimeTracker.Application.Tfs;
using HonestTimeTracker.Desktop.Common;
using HonestTimeTracker.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly ITimerSwitchService _timerSwitchService;
    private readonly ILogger<TasksViewModel> _logger;
    private bool _attached;
    private bool _isSyncing;
    private string _syncStatus = string.Empty;
    private CancellationTokenSource? _syncCts;
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

    public bool IsSyncing
    {
        get => _isSyncing;
        private set => Set(ref _isSyncing, value);
    }

    public string SyncStatus
    {
        get => _syncStatus;
        private set => Set(ref _syncStatus, value);
    }

    public ICommand SyncWithTfsCommand { get; }
    public ICommand CancelSyncCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleClosedCommand { get; }
    public ICommand ToggleTodayListCommand { get; }
    public ICommand StartTimerCommand { get; }
    public ICommand StopTimerCommand { get; }

    public TasksViewModel(
        IServiceScopeFactory scopeFactory,
        ITimerStateService timerStateService,
        ITimerStopService timerStopService,
        ITimerSwitchService timerSwitchService,
        ILogger<TasksViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _timerStateService = timerStateService;
        _timerStopService = timerStopService;
        _timerSwitchService = timerSwitchService;
        _logger = logger;

        TasksView = CollectionViewSource.GetDefaultView(Tasks);
        TasksView.Filter = FilterTask;

        SyncWithTfsCommand = new AsyncRelayCommand(_ => SyncWithTfsAsync(), _ => !IsSyncing);
        CancelSyncCommand = new RelayCommand(_ => _syncCts?.Cancel(), _ => IsSyncing);
        AddCommand = new AsyncRelayCommand(_ => AddAsync());
        EditCommand = new AsyncRelayCommand(p => EditAsync((TaskDto)p!), p => p is TaskDto);
        DeleteCommand = new AsyncRelayCommand(p => DeleteAsync((TaskDto)p!), p => p is TaskDto);
        ToggleClosedCommand = new AsyncRelayCommand(p => ToggleClosedAsync((TaskDto)p!), p => p is TaskDto);
        ToggleTodayListCommand = new AsyncRelayCommand(p => ToggleTodayListAsync((TaskDto)p!), p => p is TaskDto);
        StartTimerCommand = new AsyncRelayCommand(p => StartTimerAsync((TaskDto)p!), p => p is TaskDto);
        StopTimerCommand = new AsyncRelayCommand(_ => StopTimerAsync(), p => p is TaskDto);
    }

    public void Attach()
    {
        if (_attached) return;
        _timerStateService.TimerStarted += OnTimerStateChanged;
        _timerStateService.TimerStopped += OnTimerStateChanged;
        _attached = true;
    }

    public void Detach()
    {
        if (!_attached) return;
        _timerStateService.TimerStarted -= OnTimerStateChanged;
        _timerStateService.TimerStopped -= OnTimerStateChanged;
        _attached = false;
    }

    private async void OnTimerStateChanged(object? sender, EventArgs e)
    {
        try
        {
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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

    private async Task<List<TfsCollectionDto>> GetTfsCollectionsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetTfsCollectionsQuery, List<TfsCollectionDto>>>();
        return await handler.HandleAsync(new GetTfsCollectionsQuery());
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
        var collections = await GetTfsCollectionsAsync();
        var dialog = new TaskDialog(projects, defaultPlannedHours: defaultHours, tfsCollections: collections) { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateTaskCommand, int>>();
        try
        {
            var newId = await handler.HandleAsync(new CreateTaskCommand(dialog.TaskTitle, dialog.PlannedMinutes, dialog.ProjectId, dialog.TfsWorkItemId, dialog.TfsCollectionId));
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
        var collections = await GetTfsCollectionsAsync();
        var dialog = new TaskDialog(projects, task.Title, task.PlannedMinutes, task.ProjectId, existingTfsWorkItemId: task.TfsWorkItemId, existingIsOnTodayList: task.IsOnTodayList,
            tfsCollections: collections, existingTfsCollectionId: task.TfsCollectionId)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateTaskCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new UpdateTaskCommand(task.Id, dialog.TaskTitle, dialog.PlannedMinutes, dialog.ProjectId, dialog.TfsWorkItemId, dialog.TfsCollectionId));
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

    private async Task SyncWithTfsAsync()
    {
        var result = await RunSyncAsync();
        if (result is null) return;

        await LoadAsync();

        if (result.NoCollectionsConfigured)
        {
            MessageBox.Show("No enabled TFS collections are configured.\nAdd one in Settings → TFS collections.",
                "Sync with TFS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (result.Candidates.Count == 0)
        {
            var summary = TfsSyncSummaryFormatter.Format(result);
            MessageBox.Show(summary.Length > 0 ? summary : "Everything up to date.", "Sync with TFS",
                MessageBoxButton.OK, result.Errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            return;
        }

        var projects = await GetProjectsAsync();
        var defaultHours = await GetDefaultTaskPlannedHoursAsync();
        var dialog = new TfsImportDialog(new TfsImportViewModel(result, projects, defaultHours))
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ImportTfsTasksCommand, int>>();
        try
        {
            await handler.HandleAsync(new ImportTfsTasksCommand(dialog.ImportItems));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Importing TFS work items failed");
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// Runs the sync off the UI thread. Returns null when it was cancelled or failed (the user has been informed).
    private async Task<TfsSyncResult?> RunSyncAsync()
    {
        using var cts = new CancellationTokenSource();
        _syncCts = cts;
        IsSyncing = true;
        SyncStatus = "Syncing with TFS…";
        var progress = new Progress<string>(status => SyncStatus = status);
        try
        {
            return await Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SyncWithTfsCommand, TfsSyncResult>>();
                return await handler.HandleAsync(new SyncWithTfsCommand(progress), cts.Token);
            }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TFS sync cancelled by the user");
            await LoadAsync();
            MessageBox.Show("TFS sync cancelled.", "Sync with TFS", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TFS sync failed");
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }
        finally
        {
            _syncCts = null;
            IsSyncing = false;
            SyncStatus = string.Empty;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private Task StartTimerAsync(TaskDto task) => _timerSwitchService.SwitchToAsync(task, confirm: true);

    private Task StopTimerAsync() => _timerStopService.SafeStopAsync();
}
