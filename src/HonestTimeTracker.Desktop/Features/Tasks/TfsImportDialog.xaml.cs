using HonestTimeTracker.Application.Tfs;
using System.Windows;

namespace HonestTimeTracker.Desktop.Features.Tasks;

public partial class TfsImportDialog : Window
{
    private readonly TfsImportViewModel _viewModel;

    public List<ImportTfsTaskItem> ImportItems { get; private set; } = [];

    public TfsImportDialog(TfsImportViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanImport) return;
        ImportItems = _viewModel.BuildImportItems();
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
