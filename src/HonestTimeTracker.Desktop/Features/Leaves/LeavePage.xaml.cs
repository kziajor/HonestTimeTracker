namespace HonestTimeTracker.Desktop.Features.Leaves;

public partial class LeavePage : System.Windows.Controls.UserControl
{
    private readonly LeaveViewModel _vm;

    public LeavePage(LeaveViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    public async Task InitializeAsync() => await _vm.LoadAsync();
}
