namespace HonestTimeTracker.MigrationTool.ConsoleDb;

internal sealed class CttProject
{
    public int PR_Id { get; set; }
    public string PR_Name { get; set; } = string.Empty;
    public bool PR_Closed { get; set; }
}

internal sealed class CttTask
{
    public int TA_Id { get; set; }
    public string TA_Title { get; set; } = string.Empty;
    public int TA_PlannedTime { get; set; }
    public bool TA_Closed { get; set; }
    public int TA_RelProjectId { get; set; }
    public int TA_SourceType { get; set; }
    public string TA_SourceTaskId { get; set; } = string.Empty;
}

internal sealed class CttRecord
{
    public int RE_Id { get; set; }
    public int RE_RelTaskId { get; set; }
    public DateTime RE_StartedAt { get; set; }
    public DateTime? RE_FinishedAt { get; set; }
    public int RE_MinutesSpent { get; set; }
    public string? RE_Comment { get; set; }
}
