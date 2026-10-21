using System.Windows.Controls;

namespace HonestTimeTracker.Desktop.Features.Records;

public partial class RecordsPage : UserControl
{
    private readonly RecordsViewModel _vm;

    public RecordsPage(RecordsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += (_, _) => _vm.Attach();
        Unloaded += (_, _) =>
        {
            _vm.Detach();
            _vm.StopTicker();
        };
        ProjectSummaryListView.SizeChanged += (_, _) => UpdateSummaryProjectColumnWidth();
    }

    public async Task InitializeAsync() => await _vm.LoadAsync();
    public async Task InitializeAsync(DateOnly date) => await _vm.LoadWithDateAsync(date);

    private void UpdateSummaryProjectColumnWidth()
    {
        const double fixedWidth = 110 + 90 + 240; // Time + Share + share bar
        var available = ProjectSummaryListView.ActualWidth - fixedWidth - 26;
        if (available > 120)
            SummaryProjectColumn.Width = available;
    }
}
