namespace HonestTimeTracker.Desktop.Services;

public class TimerStateService : ITimerStateService
{
    public event EventHandler<TimerStartedEventArgs>? TimerStarted;
    public event EventHandler? TimerStopped;

    public void NotifyTimerStarted(int taskId, string taskTitle, int previousSpentMinutes, int plannedMinutes, DateTime startedAt, int? tfsWorkItemId = null)
        => TimerStarted?.Invoke(this, new TimerStartedEventArgs(taskId, taskTitle, previousSpentMinutes, plannedMinutes, startedAt, tfsWorkItemId));

    public void NotifyTimerStopped()
        => TimerStopped?.Invoke(this, EventArgs.Empty);
}
