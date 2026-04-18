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
    }

    public async Task InitializeAsync() => await _vm.LoadAsync();
}
