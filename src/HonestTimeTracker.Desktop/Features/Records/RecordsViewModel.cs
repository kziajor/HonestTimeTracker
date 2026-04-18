using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Projects;
using HonestTimeTracker.Application.Records;
using HonestTimeTracker.Application.Tasks;
using HonestTimeTracker.Desktop.Common;
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
    private DateOnly _filterDate = DateOnly.FromDateTime(DateTime.Today);
    private bool _filterByDate = true;
    private RecordsSummaryDto? _summary;

    public ObservableCollection<RecordDto> Records { get; } = [];

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

    public RecordsViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        AddCommand = new AsyncRelayCommand(_ => AddAsync());
        EditCommand = new AsyncRelayCommand(p => EditAsync((RecordDto)p!), p => p is RecordDto);
        DeleteCommand = new AsyncRelayCommand(p => DeleteAsync((RecordDto)p!), p => p is RecordDto);
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
        var dialog = new RecordDialog(tasks) { Owner = WpfApp.Current.MainWindow };
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
        var dialog = new RecordDialog(tasks, record) { Owner = WpfApp.Current.MainWindow };
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
}
