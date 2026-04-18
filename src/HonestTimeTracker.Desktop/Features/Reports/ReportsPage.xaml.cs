using System.Windows.Controls;
using System.Windows.Input;

namespace HonestTimeTracker.Desktop.Features.Reports;

public partial class ReportsPage : UserControl
{
    private readonly ReportsViewModel _vm;

    public ReportsPage(ReportsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        _vm.NavigateToRecordsRequested += async date =>
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow mw)
                await mw.NavigateToRecordsAsync(date);
        };
    }

    public Task InitializeAsync() => Task.CompletedTask;

    private async void MonthList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as ListView)?.SelectedItem is NormMonthRow row)
            await _vm.DrillDownToMonthAsync(row.MonthNumber);
    }

    private async void DayList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as ListView)?.SelectedItem is NormDayRow row)
            await _vm.DrillDownToDayAsync(row.Date);
    }
}
