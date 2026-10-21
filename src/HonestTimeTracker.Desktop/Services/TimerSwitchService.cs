using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Records;
using HonestTimeTracker.Application.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace HonestTimeTracker.Desktop.Services;

public class TimerSwitchService(
    IServiceScopeFactory scopeFactory,
    ITimerStateService timerStateService,
    ITimerStopService timerStopService) : ITimerSwitchService
{
    public async Task<bool> SwitchToAsync(TaskDto task, bool confirm)
    {
        using (var checkScope = scopeFactory.CreateScope())
        {
            var recordRepo = checkScope.ServiceProvider.GetRequiredService<IRecordRepository>();
            var activeRecord = await recordRepo.GetActiveAsync(CancellationToken.None);

            if (activeRecord is not null)
            {
                if (activeRecord.TaskId == task.Id) return false;

                if (confirm && !ConfirmSwitch(activeRecord.Task?.Title ?? $"task #{activeRecord.TaskId}", task.Title))
                    return false;

                if (!await timerStopService.SafeStopAsync()) return false;
            }
        }

        using var scope = scopeFactory.CreateScope();
        try
        {
            var startHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler<StartTimerCommand, int>>();
            await startHandler.HandleAsync(new StartTimerCommand(task.Id));

            // Re-read the task so SpentMinutes includes the session that may have just been stopped.
            var taskRepo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
            var fresh = await taskRepo.GetByIdAsync(task.Id, CancellationToken.None);

            timerStateService.NotifyTimerStarted(
                task.Id,
                fresh?.Title ?? task.Title,
                fresh?.SpentMinutes ?? task.SpentMinutes,
                fresh?.PlannedMinutes ?? task.PlannedMinutes,
                DateTime.Now,
                fresh?.TfsWorkItemId ?? task.TfsWorkItemId);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    private static bool ConfirmSwitch(string activeTitle, string newTitle) =>
        MessageBox.Show(
            $"Timer is running for \"{activeTitle}\".\nStop it and start tracking \"{newTitle}\"?",
            "Timer already running",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes;
}
