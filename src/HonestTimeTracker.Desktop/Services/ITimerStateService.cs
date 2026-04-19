namespace HonestTimeTracker.Desktop.Services;

public class TimerStartedEventArgs(
    int taskId,
    string taskTitle,
    int previousSpentMinutes,
    int plannedMinutes,
    DateTime startedAt) : EventArgs
{
    public int TaskId { get; } = taskId;
    public string TaskTitle { get; } = taskTitle;
    public int PreviousSpentMinutes { get; } = previousSpentMinutes;
    public int PlannedMinutes { get; } = plannedMinutes;
    public DateTime StartedAt { get; } = startedAt;
}

public interface ITimerStateService
{
    event EventHandler<TimerStartedEventArgs>? TimerStarted;
    event EventHandler? TimerStopped;

    void NotifyTimerStarted(int taskId, string taskTitle, int previousSpentMinutes, int plannedMinutes, DateTime startedAt);
    void NotifyTimerStopped();
}
