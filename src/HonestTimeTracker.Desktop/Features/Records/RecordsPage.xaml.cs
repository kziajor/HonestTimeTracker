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
    }

    public async Task InitializeAsync() => await _vm.LoadAsync();
    public async Task InitializeAsync(DateOnly date) => await _vm.LoadWithDateAsync(date);
}
