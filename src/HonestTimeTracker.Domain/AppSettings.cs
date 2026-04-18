namespace HonestTimeTracker.Domain;

public class AppSettings
{
    public int Id { get; set; }
    public string DbFilePath { get; set; } = string.Empty;
    public double DailyWorkHours { get; set; } = 8.0;
}
