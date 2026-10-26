namespace HonestTimeTracker.Domain;

/// Remembers which application project was last chosen for work items imported from a given TFS project.
public class TfsImportPreference
{
    public int Id { get; set; }

    public int TfsCollectionId { get; set; }
    public string TfsProjectName { get; set; } = string.Empty;
    public int LastProjectId { get; set; }

    public bool IsDeleted { get; set; }
}
