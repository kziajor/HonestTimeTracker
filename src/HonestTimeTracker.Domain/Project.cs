namespace HonestTimeTracker.Domain;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Closed { get; set; }

    public int? TfsCollectionId { get; set; }
    public TfsCollection? TfsCollection { get; set; }
    public string? TfsProjectId { get; set; }

    public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
}
