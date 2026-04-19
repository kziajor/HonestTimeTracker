namespace HonestTimeTracker.Domain;

public class AppSettings
{
    public int Id { get; set; }
    public string DbFilePath { get; set; } = string.Empty;
    public double DailyWorkHours { get; set; } = 8.0;
    public double? DefaultTaskPlannedHours { get; set; }
    public bool ShowFloatingTimer { get; set; } = true;
    public double? FloatingTimerLeft { get; set; }
    public double? FloatingTimerTop { get; set; }
}
