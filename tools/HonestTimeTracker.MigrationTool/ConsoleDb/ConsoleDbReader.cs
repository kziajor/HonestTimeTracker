using Dapper;
using Microsoft.Data.Sqlite;

namespace HonestTimeTracker.MigrationTool.ConsoleDb;

internal sealed class ConsoleDbReader(string dbPath)
{
    public async Task<(List<CttProject> Projects, List<CttTask> Tasks, List<CttRecord> Records)> ReadAllAsync()
    {
        await using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        await conn.OpenAsync();

        var projects = (await conn.QueryAsync<CttProject>(
            "SELECT PR_Id, PR_Name, PR_Closed FROM Projects")).AsList();

        var tasks = (await conn.QueryAsync<CttTask>(
            "SELECT TA_Id, TA_Title, TA_PlannedTime, TA_Closed, TA_RelProjectId, TA_SourceType, TA_SourceTaskId FROM Tasks")).AsList();

        var records = (await conn.QueryAsync<CttRecord>(
            "SELECT RE_Id, RE_RelTaskId, RE_StartedAt, RE_FinishedAt, RE_MinutesSpent, RE_Comment FROM Records")).AsList();

        return (projects, tasks, records);
    }
}
