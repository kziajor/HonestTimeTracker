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
using WpfApp = System.Windows.Application;

namespace HonestTimeTracker.Desktop.Features.Records;

public class RecordsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITimerStateService _timerStateService;
    private readonly ITimerStopService _timerStopService;
    private DateOnly _filterDate = DateOnly.FromDateTime(DateTime.Today);
    private bool _filterByDate = true;
    private RecordsSummaryDto? _summary;
    private RecordDto? _activeRecord;

    public ObservableCollection<RecordDto> Records { get; } = [];

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

    public RecordsSummaryDto? Summary
    {
        get => _summary;
        private set
        {
            if (Set(ref _summary, value))
            {
                OnPropertyChanged(nameof(HasSummary));
                OnPropertyChanged(nameof(SummaryActual));
                OnPropertyChanged(nameof(SummaryRequired));
                OnPropertyChanged(nameof(SummaryBalance));
                OnPropertyChanged(nameof(SummaryIsPositive));
                OnPropertyChanged(nameof(SummaryWorkingDays));
            }
        }
    }

    public bool HasSummary => _summary != null;
    public string SummaryActual => _summary != null ? _summary.ActualHours.ToString("F2", CultureInfo.InvariantCulture) + " h" : "";
    public string SummaryRequired => _summary != null ? _summary.RequiredHours.ToString("F2", CultureInfo.InvariantCulture) + " h" : "";
    public string SummaryBalance => _summary != null
        ? (_summary.OvertimeHours >= 0 ? "+" : "") + _summary.OvertimeHours.ToString("F2", CultureInfo.InvariantCulture) + " h"
        : "";
    public bool SummaryIsPositive => _summary?.OvertimeHours >= 0;
    public string SummaryWorkingDays => _summary != null ? $"{_summary.WorkingDays} working days" : "";

    public DateOnly FilterDate
    {
        get => _filterDate;
        set { if (Set(ref _filterDate, value)) _ = LoadAsync(); }
    }

    public bool FilterByDate
    {
        get => _filterByDate;
        set { if (Set(ref _filterByDate, value)) _ = LoadAsync(); }
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
        AddCommand = new AsyncRelayCommand(_ => AddAsync());
        EditCommand = new AsyncRelayCommand(p => EditAsync((RecordDto)p!), p => p is RecordDto);
        DeleteCommand = new AsyncRelayCommand(p => DeleteAsync((RecordDto)p!), p => p is RecordDto);
        StopTimerCommand = new AsyncRelayCommand(_ => StopAsync());
        PreviousDayCommand = new RelayCommand(_ => FilterDate = FilterDate.AddDays(-1), _ => FilterByDate);
        NextDayCommand = new RelayCommand(_ => FilterDate = FilterDate.AddDays(1), _ => FilterByDate);
    }

    public async Task LoadWithDateAsync(DateOnly date)
    {
        _filterByDate = true;
        _filterDate = date;
        OnPropertyChanged(nameof(FilterByDate));
        OnPropertyChanged(nameof(FilterDate));
        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetRecordsQuery, List<RecordDto>>>();
        var date = _filterByDate ? _filterDate : (DateOnly?)null;
        var records = await handler.HandleAsync(new GetRecordsQuery(Date: date));
        Records.Clear();
        foreach (var r in records) Records.Add(r);

        var summaryHandler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetRecordsSummaryQuery, RecordsSummaryDto?>>();
        DateOnly? from = _filterByDate ? _filterDate : null;
        DateOnly? to = _filterByDate ? _filterDate : null;
        Summary = await summaryHandler.HandleAsync(new GetRecordsSummaryQuery(from, to));

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
            active.Comment);
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
        var defaultDate = _filterByDate ? (DateTime?)_filterDate.ToDateTime(TimeOnly.MinValue) : null;
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
                        task.Id, task.Title, task.SpentMinutes, task.PlannedMinutes, dialog.StartedAt);
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
                    // For completed→running: handler subtracted old minutes from the task
                    var previousSpent = wasRunning
                        ? task.SpentMinutes
                        : Math.Max(0, task.SpentMinutes - record.MinutesSpent);
                    _timerStateService.NotifyTimerStarted(
                        task.Id, task.Title, previousSpent, task.PlannedMinutes, dialog.StartedAt);
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
}
