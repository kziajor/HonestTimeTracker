using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Tasks;
using HonestTimeTracker.Desktop.Common;
using HonestTimeTracker.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HonestTimeTracker.Desktop.Features.Today;

public class TodayViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITimerStateService _timerStateService;
    private readonly ITimerStopService _timerStopService;
    private readonly ITimerSwitchService _timerSwitchService;
    private bool _attached;

    public ObservableCollection<TaskDto> Tasks { get; } = [];

    public ICommand RemoveFromTodayCommand { get; }
    public ICommand StartTimerCommand { get; }
    public ICommand StopTimerCommand { get; }
    public ICommand CloseTaskCommand { get; }

    public TodayViewModel(
        IServiceScopeFactory scopeFactory,
        ITimerStateService timerStateService,
        ITimerStopService timerStopService,
        ITimerSwitchService timerSwitchService)
    {
        _scopeFactory = scopeFactory;
        _timerStateService = timerStateService;
        _timerStopService = timerStopService;
        _timerSwitchService = timerSwitchService;
        RemoveFromTodayCommand = new AsyncRelayCommand(p => RemoveFromTodayAsync((TaskDto)p!), p => p is TaskDto);
        StartTimerCommand = new AsyncRelayCommand(p => StartTimerAsync((TaskDto)p!), p => p is TaskDto);
        StopTimerCommand = new AsyncRelayCommand(_ => StopTimerAsync(), p => p is TaskDto);
        CloseTaskCommand = new AsyncRelayCommand(p => CloseTaskAsync((TaskDto)p!), p => p is TaskDto);
    }

    public void Attach()
    {
        if (_attached) return;
        _timerStateService.TimerStarted += OnTimerStateChanged;
        _timerStateService.TimerStopped += OnTimerStateChanged;
        _attached = true;
    }

    public void Detach()
    {
        if (!_attached) return;
        _timerStateService.TimerStarted -= OnTimerStateChanged;
        _timerStateService.TimerStopped -= OnTimerStateChanged;
        _attached = false;
    }

    private async void OnTimerStateChanged(object? sender, EventArgs e)
    {
        try
        {
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public async Task LoadAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetTodayTasksQuery, List<TaskDto>>>();
        var tasks = await handler.HandleAsync(new GetTodayTasksQuery());
        Tasks.Clear();
        foreach (var t in tasks) Tasks.Add(t);
    }

    private async Task RemoveFromTodayAsync(TaskDto task)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SetTodayListCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new SetTodayListCommand(task.Id, IsOnTodayList: false));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private Task StartTimerAsync(TaskDto task) => _timerSwitchService.SwitchToAsync(task, confirm: true);

    private Task StopTimerAsync() => _timerStopService.SafeStopAsync();

    private async Task CloseTaskAsync(TaskDto task)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ToggleTaskClosedCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new ToggleTaskClosedCommand(task.Id));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
