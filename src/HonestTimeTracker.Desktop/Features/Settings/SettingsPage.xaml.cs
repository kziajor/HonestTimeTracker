using System.Windows.Controls;

namespace HonestTimeTracker.Desktop.Features.Settings;

public partial class SettingsPage : UserControl
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public Task InitializeAsync() => _viewModel.LoadAsync();
}
