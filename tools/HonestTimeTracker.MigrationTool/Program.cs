using HonestTimeTracker.Infrastructure.Persistence;
using HonestTimeTracker.MigrationTool.ConsoleDb;
using HonestTimeTracker.MigrationTool.Migration;
using Microsoft.EntityFrameworkCore;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: htt-migrate <path-to-ConsoleTimeTracker-database>");
    return 1;
}

var sourceDbPath = args[0];
if (!File.Exists(sourceDbPath))
{
    Console.Error.WriteLine($"Source database not found: {sourceDbPath}");
    return 1;
}

var targetDbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "HonestTimeTracker",
    "htt.db");

Directory.CreateDirectory(Path.GetDirectoryName(targetDbPath)!);

Console.WriteLine($"Source : {sourceDbPath}");
Console.WriteLine($"Target : {targetDbPath}");
Console.WriteLine();

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={targetDbPath}")
    .Options;

await using var db = new AppDbContext(options);
await db.Database.MigrateAsync();

var hasExistingData =
    await db.Projects.AnyAsync() ||
    await db.Tasks.AnyAsync() ||
    await db.Records.AnyAsync();

if (hasExistingData)
    Console.WriteLine("[WARN] Target database already contains data. New records will be appended.");

Console.WriteLine("Reading source database...");
var reader = new ConsoleDbReader(sourceDbPath);
var (projects, tasks, records) = await reader.ReadAllAsync();
Console.WriteLine($"  Found: {projects.Count} projects, {tasks.Count} tasks, {records.Count} records");
Console.WriteLine();

Console.WriteLine("Migrating...");
var runner = new MigrationRunner(db);
var (migratedProjects, migratedTasks, migratedRecords) = await runner.RunAsync(projects, tasks, records);

Console.WriteLine();
Console.WriteLine($"Done. Migrated: {migratedProjects} projects, {migratedTasks} tasks, {migratedRecords} records.");
return 0;
