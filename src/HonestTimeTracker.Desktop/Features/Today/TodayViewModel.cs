using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Records;
using HonestTimeTracker.Application.Tasks;
using HonestTimeTracker.Desktop.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HonestTimeTracker.Desktop.Features.Today;

public class TodayViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ObservableCollection<TaskDto> Tasks { get; } = [];

    public ICommand RemoveFromTodayCommand { get; }
    public ICommand StartTimerCommand { get; }
    public ICommand StopTimerCommand { get; }

    public TodayViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        RemoveFromTodayCommand = new AsyncRelayCommand(p => RemoveFromTodayAsync((TaskDto)p!), p => p is TaskDto);
        StartTimerCommand = new AsyncRelayCommand(p => StartTimerAsync((TaskDto)p!), p => p is TaskDto);
        StopTimerCommand = new AsyncRelayCommand(p => StopTimerAsync((TaskDto)p!), p => p is TaskDto);
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

    private async Task StartTimerAsync(TaskDto task)
    {
        using var checkScope = _scopeFactory.CreateScope();
        var recordRepo = checkScope.ServiceProvider.GetRequiredService<IRecordRepository>();
        var activeRecord = await recordRepo.GetActiveAsync(CancellationToken.None);

        if (activeRecord is not null && activeRecord.TaskId != task.Id)
        {
            var activeTitle = Tasks.FirstOrDefault(t => t.Id == activeRecord.TaskId)?.Title
                ?? $"task #{activeRecord.TaskId}";

            var confirm = MessageBox.Show(
                $"Timer is running for \"{activeTitle}\".\nStop it and start tracking \"{task.Title}\"?",
                "Timer already running",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            var stopHandler = checkScope.ServiceProvider.GetRequiredService<ICommandHandler<StopTimerCommand, Unit>>();
            try
            {
                await stopHandler.HandleAsync(new StopTimerCommand());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<StartTimerCommand, int>>();
        try
        {
            await handler.HandleAsync(new StartTimerCommand(task.Id));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task StopTimerAsync(TaskDto task)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<StopTimerCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new StopTimerCommand());
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
