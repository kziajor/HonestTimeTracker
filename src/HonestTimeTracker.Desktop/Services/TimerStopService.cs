using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Records;
using HonestTimeTracker.Application.Tasks;
using HonestTimeTracker.Desktop.Features.Records;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace HonestTimeTracker.Desktop.Services;

public class TimerStopService(IServiceScopeFactory scopeFactory, ITimerStateService timerStateService)
    : ITimerStopService
{
    public async Task<bool> SafeStopAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var recordRepo = scope.ServiceProvider.GetRequiredService<IRecordRepository>();
        var active = await recordRepo.GetActiveAsync(CancellationToken.None);

        if (active is null) return false;

        if (active.StartedAt.Date < DateTime.Today)
        {
            MessageBox.Show(
                "This record started on a previous day and cannot span multiple days.\nPlease set the correct end time manually.",
                "Cannot stop — multi-day record",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            var taskHandler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetTasksQuery, List<TaskDto>>>();
            var tasks = await taskHandler.HandleAsync(new GetTasksQuery(ShowClosed: false));

            var activeDto = new RecordDto(
                active.Id,
                active.TaskId,
                active.Task.Title,
                active.Task.Project?.Name,
                active.StartedAt,
                active.FinishedAt,
                active.MinutesSpent,
                active.Comment);

            var dialog = new RecordDialog(tasks, activeDto, allowRunning: false)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (dialog.ShowDialog() != true) return false;

            var updateHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateRecordCommand, Unit>>();
            try
            {
                await updateHandler.HandleAsync(new UpdateRecordCommand(
                    active.Id,
                    dialog.SelectedTaskId,
                    dialog.StartedAt,
                    dialog.FinishedAt,
                    dialog.Comment));
                timerStateService.NotifyTimerStopped();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        var stopHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler<StopTimerCommand, Unit>>();
        try
        {
            await stopHandler.HandleAsync(new StopTimerCommand());
            timerStateService.NotifyTimerStopped();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }
}
