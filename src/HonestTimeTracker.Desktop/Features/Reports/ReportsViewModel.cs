using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Reports;
using HonestTimeTracker.Desktop.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;

namespace HonestTimeTracker.Desktop.Features.Reports;

public record MonthItem(int? Value, string Label);

public record NormDayRow(DateOnly Date, bool IsWeekend, bool IsLeave, double RequiredHours, double ActualHours)
{
    public string DateLabel => Date.ToString("yyyy-MM-dd");
    public string DayName => Date.ToString("ddd", CultureInfo.InvariantCulture);
    public string Type => IsWeekend ? "Weekend" : IsLeave ? "Leave" : "Work";
    public double DeltaHours => Math.Round(ActualHours - RequiredHours, 2);
    public string RequiredLabel => RequiredHours.ToString("F2", CultureInfo.InvariantCulture);
    public string ActualLabel => ActualHours.ToString("F2", CultureInfo.InvariantCulture);
    public string DeltaLabel => (DeltaHours >= 0 ? "+" : "") + DeltaHours.ToString("F2", CultureInfo.InvariantCulture);
    public bool IsDeltaPositive => DeltaHours > 0.005;
    public bool IsDeltaNegative => DeltaHours < -0.005;
}

public record NormMonthRow(int MonthNumber, int Year, int WorkingDays, int LeaveDays, double RequiredHours, double ActualHours)
{
    public string MonthName => new DateOnly(Year, MonthNumber, 1).ToString("MMMM yyyy");
    public double DeltaHours => Math.Round(ActualHours - RequiredHours, 2);
    public string RequiredLabel => RequiredHours.ToString("F2", CultureInfo.InvariantCulture);
    public string ActualLabel => ActualHours.ToString("F2", CultureInfo.InvariantCulture);
    public string DeltaLabel => (DeltaHours >= 0 ? "+" : "") + DeltaHours.ToString("F2", CultureInfo.InvariantCulture);
    public bool IsDeltaPositive => DeltaHours > 0.005;
    public bool IsDeltaNegative => DeltaHours < -0.005;
}

public class ReportsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    private string _year = DateTime.Today.Year.ToString();
    public string Year
    {
        get => _year;
        set => Set(ref _year, value);
    }

    private MonthItem _selectedMonth = null!;
    public MonthItem SelectedMonth
    {
        get => _selectedMonth;
        set => Set(ref _selectedMonth, value);
    }

    public List<MonthItem> Months { get; } =
    [
        new MonthItem(null, "Full year"),
        new MonthItem(1, "January"),
        new MonthItem(2, "February"),
        new MonthItem(3, "March"),
        new MonthItem(4, "April"),
        new MonthItem(5, "May"),
        new MonthItem(6, "June"),
        new MonthItem(7, "July"),
        new MonthItem(8, "August"),
        new MonthItem(9, "September"),
        new MonthItem(10, "October"),
        new MonthItem(11, "November"),
        new MonthItem(12, "December"),
    ];

    private bool _hasReport;
    public bool HasReport
    {
        get => _hasReport;
        set => Set(ref _hasReport, value);
    }

    private bool _isYearMode;
    public bool IsYearMode
    {
        get => _isYearMode;
        set => Set(ref _isYearMode, value);
    }

    private string _requiredHours = "0.00";
    public string RequiredHours
    {
        get => _requiredHours;
        set => Set(ref _requiredHours, value);
    }

    private string _actualHours = "0.00";
    public string ActualHours
    {
        get => _actualHours;
        set => Set(ref _actualHours, value);
    }

    private string _overtimeLabel = "+0.00 h";
    public string OvertimeLabel
    {
        get => _overtimeLabel;
        set => Set(ref _overtimeLabel, value);
    }

    private bool _isOvertime;
    public bool IsOvertime
    {
        get => _isOvertime;
        set => Set(ref _isOvertime, value);
    }

    private bool _isDeficit;
    public bool IsDeficit
    {
        get => _isDeficit;
        set => Set(ref _isDeficit, value);
    }

    private int _workingDays;
    public int WorkingDays
    {
        get => _workingDays;
        set => Set(ref _workingDays, value);
    }

    private int _leaveDays;
    public int LeaveDays
    {
        get => _leaveDays;
        set => Set(ref _leaveDays, value);
    }

    public ObservableCollection<NormDayRow> DayRows { get; } = [];
    public ObservableCollection<NormMonthRow> MonthRows { get; } = [];

    public ICommand CalculateCommand { get; }
    public ICommand ExportCommand { get; }

    public event Func<DateOnly, Task>? NavigateToRecordsRequested;

    public ReportsViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _selectedMonth = Months.First(m => m.Value == DateTime.Today.Month);
        CalculateCommand = new AsyncRelayCommand(_ => CalculateAsync());
        ExportCommand = new AsyncRelayCommand(_ => ExportAsync(), _ => HasReport);
    }

    public async Task DrillDownToMonthAsync(int month)
    {
        SelectedMonth = Months.First(m => m.Value == month);
        await CalculateAsync();
    }

    public async Task DrillDownToDayAsync(DateOnly date)
    {
        if (NavigateToRecordsRequested is not null)
            await NavigateToRecordsRequested(date);
    }

    private async Task CalculateAsync()
    {
        if (!int.TryParse(Year, out var year) || year < 2000 || year > 2100)
        {
            System.Windows.MessageBox.Show("Enter a valid year (2000–2100).", "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetNormReportQuery, NormReportDto>>();
        var report = await handler.HandleAsync(new GetNormReportQuery(year, SelectedMonth.Value));

        var overtime = report.OvertimeHours;
        OvertimeLabel = (overtime >= 0 ? "+" : "") + overtime.ToString("F2", CultureInfo.InvariantCulture) + " h";
        RequiredHours = report.RequiredHours.ToString("F2", CultureInfo.InvariantCulture);
        ActualHours = report.ActualHours.ToString("F2", CultureInfo.InvariantCulture);
        IsOvertime = overtime > 0.005;
        IsDeficit = overtime < -0.005;
        WorkingDays = report.WorkingDays;
        LeaveDays = report.LeaveDays;
        IsYearMode = !report.Month.HasValue;
        HasReport = true;

        DayRows.Clear();
        MonthRows.Clear();

        if (IsYearMode)
        {
            var grouped = report.Days
                .GroupBy(d => d.Date.Month)
                .OrderBy(g => g.Key)
                .Select(g => new NormMonthRow(
                    g.Key,
                    year,
                    g.Count(d => !d.IsWeekend && !d.IsLeave),
                    g.Count(d => d.IsLeave),
                    g.Sum(d => d.RequiredHours),
                    Math.Round(g.Sum(d => d.ActualHours), 2)));
            foreach (var row in grouped) MonthRows.Add(row);
        }
        else
        {
            foreach (var d in report.Days)
                DayRows.Add(new NormDayRow(d.Date, d.IsWeekend, d.IsLeave, d.RequiredHours, Math.Round(d.ActualHours, 2)));
        }
    }

    private async Task ExportAsync()
    {
        if (!int.TryParse(Year, out var year)) return;

        var defaultName = SelectedMonth.Value.HasValue
            ? $"HonestTimeTracker_Report_{year:D4}-{SelectedMonth.Value.Value:D2}.xlsx"
            : $"HonestTimeTracker_Report_{year:D4}.xlsx";

        var dialog = new SaveFileDialog
        {
            Filter = "Excel files (*.xlsx)|*.xlsx",
            FileName = defaultName,
            DefaultExt = ".xlsx",
            AddExtension = true,
            OverwritePrompt = true,
        };

        if (dialog.ShowDialog() != true) return;

        var path = dialog.FileName;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var reportHandler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetNormReportQuery, NormReportDto>>();
            var recordsHandler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetExportRecordsQuery, List<ExportRecordDto>>>();

            var month = SelectedMonth.Value;
            var report = await reportHandler.HandleAsync(new GetNormReportQuery(year, month));
            var records = await recordsHandler.HandleAsync(new GetExportRecordsQuery(year, month));

            ReportExcelExporter.Export(path, records, report);

            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Export failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
