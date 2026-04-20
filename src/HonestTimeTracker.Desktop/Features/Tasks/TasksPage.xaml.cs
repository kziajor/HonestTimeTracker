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
        // Fixed: TFS ID(70) + Plan(70) + Spent(70) + Status(75) + Actions(130) + scrollbar/border(26)
        const double fixedWidth = 70 + 70 + 70 + 75 + 130 + 26;
        var available = TaskListView.ActualWidth - fixedWidth;
        if (available < 120) return;
        TitleColumn.Width = Math.Round(available * 0.60);
        ProjectColumn.Width = Math.Round(available * 0.40);
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
