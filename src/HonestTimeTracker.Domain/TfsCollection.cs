namespace HonestTimeTracker.Domain;

public class TfsCollection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
