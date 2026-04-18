using System.Windows.Controls;

namespace HonestTimeTracker.Desktop.Features.Projects;

public partial class ProjectsPage : UserControl
{
    private readonly ProjectsViewModel _vm;

    public ProjectsPage(ProjectsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        ProjectListView.SizeChanged += (_, _) => UpdateNameColumnWidth();
    }

    public async Task InitializeAsync() => await _vm.LoadAsync();

    private void UpdateNameColumnWidth()
    {
        const double fixedWidth = 120 + 120; // Status + Akcje
        var available = ProjectListView.ActualWidth - fixedWidth - 26;
        if (available > 80)
            NameColumn.Width = available;
    }
}
