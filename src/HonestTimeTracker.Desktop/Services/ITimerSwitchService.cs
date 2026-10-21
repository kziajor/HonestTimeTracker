using HonestTimeTracker.Application.Tasks;

namespace HonestTimeTracker.Desktop.Services;

public interface ITimerSwitchService
{
    /// Starts tracking the given task. If another task is being tracked, it is stopped first
    /// (optionally after user confirmation). Returns true if the timer was started.
    Task<bool> SwitchToAsync(TaskDto task, bool confirm);
}
