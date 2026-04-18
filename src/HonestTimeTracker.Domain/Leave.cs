namespace HonestTimeTracker.Domain;

public class Leave
{
    public int Id { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Description { get; set; }
    public bool IsDeleted { get; set; }
}
