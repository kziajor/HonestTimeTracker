using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Records;
using HonestTimeTracker.Application.Settings;
using HonestTimeTracker.Desktop.Common;
using HonestTimeTracker.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Threading;

namespace HonestTimeTracker.Desktop.Features.FloatingTimer;

public class FloatingTimerViewModel : ViewModelBase
{
    private readonly ITimerStateService _timerStateService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DispatcherTimer _ticker;

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

    public string TaskTitle
    {
        get => _taskTitle;
        private set => Set(ref _taskTitle, value);
    }

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

    public FloatingTimerViewModel(ITimerStateService timerStateService, IServiceScopeFactory scopeFactory)
    {
        _timerStateService = timerStateService;
        _scopeFactory = scopeFactory;

        _ticker = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _ticker.Tick += (_, _) => RefreshDisplay();

        _timerStateService.TimerStarted += OnTimerStarted;
        _timerStateService.TimerStopped += OnTimerStopped;
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
            _taskTitle = activeRecord.Task.Title;
            SetTimerActive(true);
        }
        else
        {
            OnPropertyChanged(nameof(IsVisible));
        }
    }

    private void OnTimerStarted(object? sender, TimerStartedEventArgs e)
    {
        _taskTitle = e.TaskTitle;
        _previousSpentMinutes = e.PreviousSpentMinutes;
        _plannedMinutes = e.PlannedMinutes;
        _startedAt = e.StartedAt;
        SetTimerActive(true);
    }

    private void OnTimerStopped(object? sender, EventArgs e)
    {
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
            DiffText = string.Empty;
            IsOvertime = false;
            TaskTitle = string.Empty;
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

    private static string FormatMinutes(double totalMinutes)
    {
        var h = (int)totalMinutes / 60;
        var m = (int)totalMinutes % 60;
        return $"{h}h {m:D2}m";
    }
}
