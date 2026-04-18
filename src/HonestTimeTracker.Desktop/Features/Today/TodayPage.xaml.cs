using System.Windows.Controls;

namespace HonestTimeTracker.Desktop.Features.Today;

public partial class TodayPage : UserControl
{
    private readonly TodayViewModel _vm;

    public TodayPage(TodayViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        TaskListView.SizeChanged += (_, _) => UpdateTitleColumnWidth();
    }

    public async Task InitializeAsync() => await _vm.LoadAsync();

    private void UpdateTitleColumnWidth()
    {
        const double fixedWidth = 160 + 90 + 120 + 80; // Project + Plan + Spent + Actions
        var available = TaskListView.ActualWidth - fixedWidth - 26;
        if (available > 80)
            TitleColumn.Width = available;
    }
}
