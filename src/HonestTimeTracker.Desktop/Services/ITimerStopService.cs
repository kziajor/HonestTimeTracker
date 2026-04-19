namespace HonestTimeTracker.Desktop.Services;

public interface ITimerStopService
{
    /// Stops the active timer safely. If the active record started on a previous day,
    /// shows a warning and opens the edit dialog to force manual end time entry.
    /// Returns true if the timer was stopped (caller should reload data).
    Task<bool> SafeStopAsync();
}
