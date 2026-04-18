namespace HonestTimeTracker.Domain;

public class WorkTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int PlannedMinutes { get; set; }
    public int SpentMinutes { get; set; }
    public bool Closed { get; set; }
    public bool IsOnTodayList { get; set; }

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? TfsWorkItemId { get; set; }

    public ICollection<WorkRecord> Records { get; set; } = new List<WorkRecord>();

    public bool IsDeleted { get; set; }
}
