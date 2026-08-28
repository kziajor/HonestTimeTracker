using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Projects;
using HonestTimeTracker.Application.Records;
using HonestTimeTracker.Application.Tasks;
using HonestTimeTracker.Desktop.Common;
using HonestTimeTracker.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WpfApp = System.Windows.Application;

namespace HonestTimeTracker.Desktop.Features.Records;

public class RecordsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITimerStateService _timerStateService;
    private readonly ITimerStopService _timerStopService;
    private readonly DispatcherTimer _elapsedTicker;
    private const string NoProjectLabel = "(No project)";
    private DateOnly _filterDate = DateOnly.FromDateTime(DateTime.Today);
    private RecordsSummaryDto? _summary;
    private RecordDto? _activeRecord;
    private double _projectSummaryTotalHours;

    public ObservableCollection<RecordDto> Records { get; } = [];

    public ObservableCollection<ProjectShareRow> ProjectSummary { get; } = [];

    public RecordDto? ActiveRecord
    {
        get => _activeRecord;
        private set
        {
            if (Set(ref _activeRecord, value))
            {
                OnPropertyChanged(nameof(HasActiveRecord));
                OnPropertyChanged(nameof(ActiveTaskTitle));
                OnPropertyChanged(nameof(ActiveProjectName));
                OnPropertyChanged(nameof(ActiveStartTime));
                OnPropertyChanged(nameof(ActiveElapsed));
                NotifySummaryLiveProperties();

                if (_activeRecord != null)
                    _elapsedTicker.Start();
                else
                    _elapsedTicker.Stop();
            }
        }
    }

    public bool HasActiveRecord => _activeRecord != null;
    public string ActiveTaskTitle => _activeRecord?.TaskTitle ?? "";
    public string ActiveProjectName => _activeRecord?.ProjectName ?? "";
    public string ActiveStartTime => _activeRecord != null ? _activeRecord.StartedAt.ToString("HH:mm") : "";
    public string ActiveElapsed => _activeRecord != null
        ? (DateTime.Now - _activeRecord.StartedAt).ToString(@"hh\:mm\:ss")
        : "";

    private double LiveActualHours =>
        (_summary?.ActualHours ?? 0) +
        (_activeRecord != null && _filterDate == DateOnly.FromDateTime(DateTime.Today)
            ? (DateTime.Now - _activeRecord.StartedAt).TotalHours
            : 0);

    public RecordsSummaryDto? Summary
    {
        get => _summary;
        private set
        {
            if (Set(ref _summary, value))
            {
                OnPropertyChanged(nameof(HasSummary));
                OnPropertyChanged(nameof(SummaryRequired));
                OnPropertyChanged(nameof(SummaryWorkingDays));
                NotifySummaryLiveProperties();
            }
        }
    }

    private void NotifySummaryLiveProperties()
    {
        OnPropertyChanged(nameof(SummaryActual));
        OnPropertyChanged(nameof(SummaryBalance));
        OnPropertyChanged(nameof(SummaryIsPositive));
        RefreshProjectSummary();
    }

    public bool HasSummary => _summary != null;
    public string SummaryActual => _summary != null ? LiveActualHours.ToString("F2", CultureInfo.InvariantCulture) + " h" : "";
    public string SummaryRequired => _summary != null ? _summary.RequiredHours.ToString("F2", CultureInfo.InvariantCulture) + " h" : "";
    public string SummaryBalance
    {
        get
        {
            if (_summary == null) return "";
            var overtime = LiveActualHours - _summary.RequiredHours;
            return (overtime >= 0 ? "+" : "") + overtime.ToString("F2", CultureInfo.InvariantCulture) + " h";
        }
    }
    public bool SummaryIsPositive => _summary != null && LiveActualHours >= _summary.RequiredHours;
    public string SummaryWorkingDays => _summary != null ? $"{_summary.WorkingDays} working days" : "";

    public bool HasProjectSummary => ProjectSummary.Count > 0;
    public string ProjectSummaryTotal => _projectSummaryTotalHours.ToString("F2", CultureInfo.InvariantCulture) + " h";

    private List<(string Name, bool HasProject, double Hours)> BuildProjectShareRows()
    {
        var includeLive = _filterDate == DateOnly.FromDateTime(DateTime.Today);
        var now = DateTime.Now;

        return Records
            .Select(r => new
            {
                Name = string.IsNullOrWhiteSpace(r.ProjectName) ? NoProjectLabel : r.ProjectName!,
                HasProject = !string.IsNullOrWhiteSpace(r.ProjectName),
                Hours = (r.MinutesSpent / 60.0)
                        + (r.FinishedAt is null && includeLive ? Math.Max(0, (now - r.StartedAt).TotalHours) : 0)
            })
            .GroupBy(x => x.Name)
            .Select(g => (Name: g.Key, g.First().HasProject, Hours: g.Sum(x => x.Hours)))
            .Where(x => x.Hours > 0)
            .OrderBy(x => x.HasProject ? 0 : 1)
            .ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private void RefreshProjectSummary()
    {
        var rows = BuildProjectShareRows();
        var total = rows.Sum(r => r.Hours);
        _projectSummaryTotalHours = total;

        if (total <= 0)
        {
            if (ProjectSummary.Count > 0)
            {
                ProjectSummary.Clear();
                OnPropertyChanged(nameof(HasProjectSummary));
            }
            OnPropertyChanged(nameof(ProjectSummaryTotal));
            return;
        }

        var sameShape = rows.Count == ProjectSummary.Count
            && rows.Select((r, i) => r.Name == ProjectSummary[i].ProjectName).All(match => match);

        if (sameShape)
        {
            for (var i = 0; i < rows.Count; i++)
                ProjectSummary[i].Update(rows[i].Hours, rows[i].Hours / total * 100.0);
        }
        else
        {
            ProjectSummary.Clear();
            foreach (var (name, hasProject, hours) in rows)
            {
                var row = new ProjectShareRow(name, hasProject);
                row.Update(hours, hours / total * 100.0);
                ProjectSummary.Add(row);
            }
            OnPropertyChanged(nameof(HasProjectSummary));
        }

        OnPropertyChanged(nameof(ProjectSummaryTotal));
    }

    public DateOnly FilterDate
    {
        get => _filterDate;
        set { if (Set(ref _filterDate, value)) _ = LoadAsync(); }
    }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand StopTimerCommand { get; }
    public ICommand PreviousDayCommand { get; }
    public ICommand NextDayCommand { get; }

    public RecordsViewModel(IServiceScopeFactory scopeFactory, ITimerStateService timerStateService, ITimerStopService timerStopService)
    {
        _scopeFactory = scopeFactory;
        _timerStateService = timerStateService;
        _timerStopService = timerStopService;
        _elapsedTicker = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _elapsedTicker.Tick += (_, _) =>
        {
            OnPropertyChanged(nameof(ActiveElapsed));
            NotifySummaryLiveProperties();
        };
        AddCommand = new AsyncRelayCommand(_ => AddAsync());
        EditCommand = new AsyncRelayCommand(p => EditAsync((RecordDto)p!), p => p is RecordDto r && !r.TaskClosed);
        DeleteCommand = new AsyncRelayCommand(p => DeleteAsync((RecordDto)p!), p => p is RecordDto);
        StopTimerCommand = new AsyncRelayCommand(_ => StopAsync());
        PreviousDayCommand = new RelayCommand(_ => FilterDate = FilterDate.AddDays(-1));
        NextDayCommand = new RelayCommand(_ => FilterDate = FilterDate.AddDays(1));
    }

    public async Task LoadWithDateAsync(DateOnly date)
    {
        _filterDate = date;
        OnPropertyChanged(nameof(FilterDate));
        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetRecordsQuery, List<RecordDto>>>();
        var records = await handler.HandleAsync(new GetRecordsQuery(Date: _filterDate));
        Records.Clear();
        foreach (var r in records) Records.Add(r);
        RefreshProjectSummary();

        var summaryHandler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetRecordsSummaryQuery, RecordsSummaryDto?>>();
        Summary = await summaryHandler.HandleAsync(new GetRecordsSummaryQuery(_filterDate));

        var recordRepo = scope.ServiceProvider.GetRequiredService<IRecordRepository>();
        var active = await recordRepo.GetActiveAsync(CancellationToken.None);
        ActiveRecord = active is null ? null : new RecordDto(
            active.Id,
            active.TaskId,
            active.Task.Title,
            active.Task.Project?.Name,
            active.StartedAt,
            active.FinishedAt,
            active.MinutesSpent,
            active.Comment,
            active.Task.Closed);
    }

    private async Task<List<TaskDto>> GetTasksAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetTasksQuery, List<TaskDto>>>();
        return await handler.HandleAsync(new GetTasksQuery(ShowClosed: false));
    }

    private async Task AddAsync()
    {
        var tasks = await GetTasksAsync();
        var allowRunning = _activeRecord is null;
        var defaultDate = (DateTime?)_filterDate.ToDateTime(TimeOnly.MinValue);
        Func<DateTime, DateTime?, Task<bool>> overlapChecker = async (start, end) =>
        {
            using var s = _scopeFactory.CreateScope();
            var repo = s.ServiceProvider.GetRequiredService<IRecordRepository>();
            return await repo.HasOverlapAsync(start, end, null, CancellationToken.None);
        };
        var dialog = new RecordDialog(tasks, defaultDate: defaultDate, allowRunning: allowRunning, overlapChecker: overlapChecker) { Owner = WpfApp.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateRecordCommand, int>>();
        try
        {
            await handler.HandleAsync(new CreateRecordCommand(
                dialog.SelectedTaskId,
                dialog.StartedAt,
                dialog.FinishedAt,
                dialog.Comment));

            if (!dialog.FinishedAt.HasValue)
            {
                var task = tasks.FirstOrDefault(t => t.Id == dialog.SelectedTaskId);
                if (task is not null)
                    _timerStateService.NotifyTimerStarted(
                        task.Id, task.Title, task.SpentMinutes, task.PlannedMinutes, dialog.StartedAt, task.TfsWorkItemId);
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task EditAsync(RecordDto record)
    {
        var tasks = await GetTasksAsync();
        var allowRunning = _activeRecord is null || _activeRecord.Id == record.Id;
        var wasRunning = !record.FinishedAt.HasValue;
        var recordId = record.Id;
        Func<DateTime, DateTime?, Task<bool>> overlapChecker = async (start, end) =>
        {
            using var s = _scopeFactory.CreateScope();
            var repo = s.ServiceProvider.GetRequiredService<IRecordRepository>();
            return await repo.HasOverlapAsync(start, end, recordId, CancellationToken.None);
        };
        var dialog = new RecordDialog(tasks, record, allowRunning: allowRunning, overlapChecker: overlapChecker) { Owner = WpfApp.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateRecordCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new UpdateRecordCommand(
                record.Id,
                dialog.SelectedTaskId,
                dialog.StartedAt,
                dialog.FinishedAt,
                dialog.Comment));

            var isNowRunning = !dialog.FinishedAt.HasValue;

            if (wasRunning && !isNowRunning)
            {
                _timerStateService.NotifyTimerStopped();
            }
            else if (isNowRunning)
            {
                var task = tasks.FirstOrDefault(t => t.Id == dialog.SelectedTaskId);
                if (task is not null)
                {
                    var previousSpent = wasRunning
                        ? task.SpentMinutes
                        : Math.Max(0, task.SpentMinutes - record.MinutesSpent);
                    _timerStateService.NotifyTimerStarted(
                        task.Id, task.Title, previousSpent, task.PlannedMinutes, dialog.StartedAt, task.TfsWorkItemId);
                }
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task DeleteAsync(RecordDto record)
    {
        var timeRange = record.FinishedAt.HasValue
            ? $"{record.StartedAt:HH:mm}–{record.FinishedAt:HH:mm}"
            : $"{record.StartedAt:HH:mm}–running";
        var confirm = MessageBox.Show(
            $"Are you sure you want to delete this record?\n{record.TaskTitle} — {timeRange}",
            "Delete record",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeleteRecordCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new DeleteRecordCommand(record.Id));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task StopAsync()
    {
        if (await _timerStopService.SafeStopAsync())
            await LoadAsync();
    }

    internal void StopTicker() => _elapsedTicker.Stop();
}

public class ProjectShareRow(string projectName, bool hasProject) : ViewModelBase
{
    private double _hours;
    private double _percent;

    public string ProjectName { get; } = projectName;
    public bool HasProject { get; } = hasProject;

    public double Percent => _percent;
    public string TimeLabel => _hours.ToString("F2", CultureInfo.InvariantCulture) + " h";
    public string PercentLabel => _percent.ToString("F1", CultureInfo.InvariantCulture) + "%";

    public void Update(double hours, double percent)
    {
        Set(ref _hours, hours, nameof(TimeLabel));
        if (Set(ref _percent, percent, nameof(Percent)))
            OnPropertyChanged(nameof(PercentLabel));
    }
}
