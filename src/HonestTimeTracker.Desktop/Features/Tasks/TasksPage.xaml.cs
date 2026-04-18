using System.Windows;
using System.Windows.Controls;

namespace HonestTimeTracker.Desktop.Features.Tasks;

public partial class TasksPage : UserControl
{
    private readonly TasksViewModel _vm;

    public TasksPage(TasksViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        TaskListView.SizeChanged += (_, _) => UpdateTitleColumnWidth();
    }

    public async Task InitializeAsync() => await _vm.LoadAsync();

    private void UpdateTitleColumnWidth()
    {
        const double fixedWidth = 160 + 90 + 120 + 110 + 110; // Projekt + Plan + Spędzony + Status + Akcje
        var available = TaskListView.ActualWidth - fixedWidth - 26;
        if (available > 80)
            TitleColumn.Width = available;
    }

    private void MoreActions_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu is { } menu)
        {
            menu.PlacementTarget = btn;
            menu.IsOpen = true;
        }
    }
}
