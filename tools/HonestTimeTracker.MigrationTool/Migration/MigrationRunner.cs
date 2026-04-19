using HonestTimeTracker.Domain;
using HonestTimeTracker.Infrastructure.Persistence;
using HonestTimeTracker.MigrationTool.ConsoleDb;

namespace HonestTimeTracker.MigrationTool.Migration;

internal sealed class MigrationRunner(AppDbContext db)
{
    private const int AzureSourceType = 1;

    public async Task<(int Projects, int Tasks, int Records)> RunAsync(
        List<CttProject> cttProjects,
        List<CttTask> cttTasks,
        List<CttRecord> cttRecords)
    {
        var projectIdMap = await MigrateProjectsAsync(cttProjects);
        var taskIdMap = await MigrateTasksAsync(cttTasks, cttRecords, projectIdMap);
        var recordCount = await MigrateRecordsAsync(cttRecords, taskIdMap);

        return (cttProjects.Count, cttTasks.Count, recordCount);
    }

    private async Task<Dictionary<int, int>> MigrateProjectsAsync(List<CttProject> cttProjects)
    {
        var map = new Dictionary<int, int>(cttProjects.Count);

        foreach (var ctt in cttProjects)
        {
            var project = new Project
            {
                Name = ctt.PR_Name,
                Closed = ctt.PR_Closed,
            };
            db.Projects.Add(project);
            await db.SaveChangesAsync();
            map[ctt.PR_Id] = project.Id;
        }

        return map;
    }

    private async Task<Dictionary<int, int>> MigrateTasksAsync(
        List<CttTask> cttTasks,
        List<CttRecord> cttRecords,
        Dictionary<int, int> projectIdMap)
    {
        var spentPerTask = cttRecords
            .GroupBy(r => r.RE_RelTaskId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.RE_MinutesSpent));

        var map = new Dictionary<int, int>(cttTasks.Count);

        foreach (var ctt in cttTasks)
        {
            int? tfsWorkItemId = null;
            if (ctt.TA_SourceType == AzureSourceType)
            {
                if (int.TryParse(ctt.TA_SourceTaskId, out var parsed))
                    tfsWorkItemId = parsed;
                else if (!string.IsNullOrEmpty(ctt.TA_SourceTaskId))
                    Console.WriteLine($"  [WARN] Task '{ctt.TA_Title}' (CTT id={ctt.TA_Id}): cannot parse TfsWorkItemId from '{ctt.TA_SourceTaskId}' — skipped.");
            }

            var task = new WorkTask
            {
                Title = ctt.TA_Title,
                PlannedMinutes = ctt.TA_PlannedTime,
                Closed = ctt.TA_Closed,
                ProjectId = projectIdMap.TryGetValue(ctt.TA_RelProjectId, out var newProjectId) ? newProjectId : null,
                SpentMinutes = spentPerTask.GetValueOrDefault(ctt.TA_Id, 0),
                TfsWorkItemId = tfsWorkItemId,
            };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            map[ctt.TA_Id] = task.Id;
        }

        return map;
    }

    private async Task<int> MigrateRecordsAsync(
        List<CttRecord> cttRecords,
        Dictionary<int, int> taskIdMap)
    {
        int count = 0;

        foreach (var ctt in cttRecords)
        {
            if (!taskIdMap.TryGetValue(ctt.RE_RelTaskId, out var newTaskId))
            {
                Console.WriteLine($"  [WARN] Record RE_Id={ctt.RE_Id}: parent task CTT id={ctt.RE_RelTaskId} not found — skipped.");
                continue;
            }

            var record = new WorkRecord
            {
                TaskId = newTaskId,
                StartedAt = ctt.RE_StartedAt,
                FinishedAt = ctt.RE_FinishedAt,
                MinutesSpent = ctt.RE_MinutesSpent,
                Comment = ctt.RE_Comment,
            };
            db.Records.Add(record);
            count++;
        }

        await db.SaveChangesAsync();
        return count;
    }
}
