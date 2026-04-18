using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Leaves;
using HonestTimeTracker.Desktop.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WpfApp = System.Windows.Application;

namespace HonestTimeTracker.Desktop.Features.Leaves;

public class LeaveViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ObservableCollection<LeaveDto> Leaves { get; } = [];

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public LeaveViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        AddCommand = new AsyncRelayCommand(_ => AddAsync());
        EditCommand = new AsyncRelayCommand(p => EditAsync((LeaveDto)p!), p => p is LeaveDto);
        DeleteCommand = new AsyncRelayCommand(p => DeleteAsync((LeaveDto)p!), p => p is LeaveDto);
    }

    public async Task LoadAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetLeavesQuery, List<LeaveDto>>>();
        var leaves = await handler.HandleAsync(new GetLeavesQuery());
        Leaves.Clear();
        foreach (var l in leaves) Leaves.Add(l);
    }

    private async Task AddAsync()
    {
        var dialog = new LeaveDialog { Owner = WpfApp.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateLeaveCommand, int>>();
        try
        {
            await handler.HandleAsync(new CreateLeaveCommand(dialog.StartDate, dialog.EndDate, dialog.Description));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task EditAsync(LeaveDto leave)
    {
        var dialog = new LeaveDialog(leave) { Owner = WpfApp.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateLeaveCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new UpdateLeaveCommand(leave.Id, dialog.StartDate, dialog.EndDate, dialog.Description));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task DeleteAsync(LeaveDto leave)
    {
        var label = leave.DaysCount == 1
            ? leave.StartDate.ToString("d MMM yyyy")
            : $"{leave.StartDate:d MMM yyyy} – {leave.EndDate:d MMM yyyy}";

        var confirm = MessageBox.Show(
            $"Are you sure you want to delete the leave entry \"{label}\"?",
            "Delete leave",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeleteLeaveCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new DeleteLeaveCommand(leave.Id));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
