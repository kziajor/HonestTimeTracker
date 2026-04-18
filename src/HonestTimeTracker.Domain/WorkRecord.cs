namespace HonestTimeTracker.Domain;

public class WorkRecord
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int MinutesSpent { get; set; }
    public string? Comment { get; set; }

    public int TaskId { get; set; }
    public WorkTask Task { get; set; } = null!;

    public bool IsDeleted { get; set; }
}
