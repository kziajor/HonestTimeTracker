using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Records;
using HonestTimeTracker.Application.Settings;
using HonestTimeTracker.Application.Tasks;
using HonestTimeTracker.Desktop.Common;
using HonestTimeTracker.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using System.Windows.Threading;

namespace HonestTimeTracker.Desktop.Features.FloatingTimer;

public record SwitchTaskItem(TaskDto Task, string Header, bool IsCurrent);

public class FloatingTimerViewModel : ViewModelBase
{
    private readonly ITimerStateService _timerStateService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITimerSwitchService _timerSwitchService;
    private readonly ITimerStopService _timerStopService;
    private readonly DispatcherTimer _ticker;

    private int? _activeTaskId;

    private string _taskTitle = string.Empty;
    private int _previousSpentMinutes;
    private int _plannedMinutes;
    private DateTime _startedAt;
    private bool _isTimerActive;
    private bool _showFloatingTimer = true;
    private string _totalSpentText = string.Empty;
    private string _plannedText = string.Empty;
    private string _diffText = string.Empty;
    private bool _isOvertime;
    private string _tfsWorkItemText = string.Empty;
    private double? _dayRequiredMinutes;
    private double _dayFinishedMinutes;
    private string _dayLeftText = string.Empty;
    private bool _isDayComplete;

    public string TaskTitle
    {
        get => _taskTitle;
        private set => Set(ref _taskTitle, value);
    }

    public string TfsWorkItemText
    {
        get => _tfsWorkItemText;
        private set
        {
            if (Set(ref _tfsWorkItemText, value))
                OnPropertyChanged(nameof(IsTfsVisible));
        }
    }

    public bool IsTfsVisible => !string.IsNullOrEmpty(_tfsWorkItemText);

    public string TotalSpentText
    {
        get => _totalSpentText;
        private set => Set(ref _totalSpentText, value);
    }

    public string PlannedText
    {
        get => _plannedText;
        private set => Set(ref _plannedText, value);
    }

    public string DiffText
    {
        get => _diffText;
        private set
        {
            if (Set(ref _diffText, value))
                OnPropertyChanged(nameof(IsDiffVisible));
        }
    }

    public string DayLeftText
    {
        get => _dayLeftText;
        private set => Set(ref _dayLeftText, value);
    }

    public bool IsDayComplete
    {
        get => _isDayComplete;
        private set => Set(ref _isDayComplete, value);
    }

    public bool IsOvertime
    {
        get => _isOvertime;
        private set => Set(ref _isOvertime, value);
    }

    public bool IsDiffVisible => !string.IsNullOrEmpty(_diffText) && _isTimerActive;

    private double _progressPercent;
    private string _progressPercentText = string.Empty;

    public double ProgressPercent
    {
        get => _progressPercent;
        private set => Set(ref _progressPercent, value);
    }

    public string ProgressPercentText
    {
        get => _progressPercentText;
        private set => Set(ref _progressPercentText, value);
    }

    public bool IsVisible => _isTimerActive && _showFloatingTimer;

    public bool ShowFloatingTimer
    {
        get => _showFloatingTimer;
        set
        {
            if (Set(ref _showFloatingTimer, value))
                OnPropertyChanged(nameof(IsVisible));
        }
    }

    public ICommand SwitchTaskCommand { get; }
    public ICommand StopTimerCommand { get; }

    public FloatingTimerViewModel(
        ITimerStateService timerStateService,
        IServiceScopeFactory scopeFactory,
        ITimerSwitchService timerSwitchService,
        ITimerStopService timerStopService)
    {
        _timerStateService = timerStateService;
        _scopeFactory = scopeFactory;
        _timerSwitchService = timerSwitchService;
        _timerStopService = timerStopService;

        SwitchTaskCommand = new AsyncRelayCommand(
            p => _timerSwitchService.SwitchToAsync((TaskDto)p!, confirm: false),
            p => p is TaskDto t && t.Id != _activeTaskId);
        StopTimerCommand = new AsyncRelayCommand(_ => _timerStopService.SafeStopAsync(), _ => _isTimerActive);

        _ticker = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _ticker.Tick += (_, _) => RefreshDisplay();

        _timerStateService.TimerStarted += OnTimerStarted;
        _timerStateService.TimerStopped += OnTimerStopped;
        _timerStateService.RecordsChanged += OnRecordsChanged;
    }

    public async Task InitializeAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var settingsHandler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetSettingsQuery, SettingsDto>>();
        var settings = await settingsHandler.HandleAsync(new GetSettingsQuery());
        _showFloatingTimer = settings.ShowFloatingTimer;
        OnPropertyChanged(nameof(ShowFloatingTimer));

        var recordRepo = scope.ServiceProvider.GetRequiredService<IRecordRepository>();
        var activeRecord = await recordRepo.GetActiveAsync(CancellationToken.None);
        if (activeRecord?.Task is not null)
        {
            _previousSpentMinutes = activeRecord.Task.SpentMinutes;
            _plannedMinutes = activeRecord.Task.PlannedMinutes;
            _startedAt = activeRecord.StartedAt;
            _activeTaskId = activeRecord.TaskId;
            TaskTitle = activeRecord.Task.Title;
            TfsWorkItemText = activeRecord.Task.TfsWorkItemId.HasValue ? $"TFS #{activeRecord.Task.TfsWorkItemId}" : string.Empty;
            SetTimerActive(true);
            await LoadDaySummaryAsync();
        }
        else
        {
            OnPropertyChanged(nameof(IsVisible));
        }
    }

    private void OnTimerStarted(object? sender, TimerStartedEventArgs e)
    {
        TaskTitle = e.TaskTitle;
        _previousSpentMinutes = e.PreviousSpentMinutes;
        _plannedMinutes = e.PlannedMinutes;
        _startedAt = e.StartedAt;
        _activeTaskId = e.TaskId;
        TfsWorkItemText = e.TfsWorkItemId.HasValue ? $"TFS #{e.TfsWorkItemId}" : string.Empty;
        SetTimerActive(true);
        _ = LoadDaySummaryAsync();
    }

    private void OnRecordsChanged(object? sender, EventArgs e)
    {
        if (_isTimerActive)
            _ = ReloadAfterRecordsChangedAsync();
    }

    private async Task ReloadAfterRecordsChangedAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var recordRepo = scope.ServiceProvider.GetRequiredService<IRecordRepository>();
            var activeRecord = await recordRepo.GetActiveAsync(CancellationToken.None);
            if (activeRecord?.Task is not null && activeRecord.TaskId == _activeTaskId)
            {
                _previousSpentMinutes = activeRecord.Task.SpentMinutes;
                _plannedMinutes = activeRecord.Task.PlannedMinutes;
                _startedAt = activeRecord.StartedAt;
            }
        }
        catch (Exception)
        {
            // Keep the current values; the next timer start reloads them anyway.
        }

        await LoadDaySummaryAsync();
    }

    private async Task LoadDaySummaryAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetRecordsSummaryQuery, RecordsSummaryDto?>>();
            var summary = await handler.HandleAsync(new GetRecordsSummaryQuery(DateOnly.FromDateTime(DateTime.Today)));
            _dayRequiredMinutes = summary?.RequiredHours * 60;
            _dayFinishedMinutes = (summary?.ActualHours ?? 0) * 60;
        }
        catch (Exception)
        {
            _dayRequiredMinutes = null;
        }

        if (_isTimerActive)
            RefreshDisplay();
    }

    public async Task<IReadOnlyList<SwitchTaskItem>> LoadMenuTasksAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetTodayTasksQuery, List<TaskDto>>>();
        var tasks = await handler.HandleAsync(new GetTodayTasksQuery());
        return tasks
            .Where(t => !t.Closed)
            .Select(t => new SwitchTaskItem(
                t,
                string.IsNullOrEmpty(t.ProjectName) ? t.Title : $"{t.Title} — {t.ProjectName}",
                t.Id == _activeTaskId))
            .ToList();
    }

    private void OnTimerStopped(object? sender, EventArgs e)
    {
        _activeTaskId = null;
        SetTimerActive(false);
    }

    private void SetTimerActive(bool active)
    {
        _isTimerActive = active;
        if (active)
        {
            RefreshDisplay();
            _ticker.Start();
        }
        else
        {
            _ticker.Stop();
            TotalSpentText = string.Empty;
            PlannedText = string.Empty;
            DayLeftText = string.Empty;
            IsDayComplete = false;
            _dayRequiredMinutes = null;
            DiffText = string.Empty;
            IsOvertime = false;
            TaskTitle = string.Empty;
            TfsWorkItemText = string.Empty;
            ProgressPercent = 0;
            ProgressPercentText = string.Empty;
        }
        OnPropertyChanged(nameof(IsVisible));
    }

    private void RefreshDisplay()
    {
        TaskTitle = _taskTitle;
        var currentSessionMinutes = (DateTime.Now - _startedAt).TotalMinutes;
        var totalMinutes = _previousSpentMinutes + currentSessionMinutes;

        TotalSpentText = FormatMinutes(totalMinutes);
        PlannedText = _plannedMinutes > 0 ? FormatMinutes(_plannedMinutes) : "—";
        RefreshDayLeft(currentSessionMinutes);

        if (_plannedMinutes > 0)
        {
            var diffMinutes = totalMinutes - _plannedMinutes;
            IsOvertime = diffMinutes > 0;
            DiffText = (IsOvertime ? "+" : "") + FormatMinutes(Math.Abs(diffMinutes));

            var pct = totalMinutes / _plannedMinutes * 100.0;
            ProgressPercent = Math.Min(pct, 100.0);
            ProgressPercentText = $"{pct:F0}%";
        }
        else
        {
            DiffText = string.Empty;
            IsOvertime = false;
            ProgressPercent = 0;
            ProgressPercentText = string.Empty;
        }
    }

    private void RefreshDayLeft(double currentSessionMinutes)
    {
        if (_dayRequiredMinutes is not { } required)
        {
            DayLeftText = "—";
            IsDayComplete = false;
            return;
        }

        // Active record is excluded from the summary (MinutesSpent == 0 while running), so add the live session.
        var leftMinutes = required - (_dayFinishedMinutes + currentSessionMinutes);
        IsDayComplete = leftMinutes <= 0;
        DayLeftText = (IsDayComplete ? "+" : "") + FormatMinutes(Math.Abs(leftMinutes));
    }

    private static string FormatMinutes(double totalMinutes)
    {
        var h = (int)totalMinutes / 60;
        var m = (int)totalMinutes % 60;
        return $"{h}h {m:D2}m";
    }
}
