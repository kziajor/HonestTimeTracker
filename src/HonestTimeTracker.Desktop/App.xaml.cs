using HonestTimeTracker.Desktop.Features.Projects;
using HonestTimeTracker.Desktop.Features.Tasks;
using HonestTimeTracker.Infrastructure;
using HonestTimeTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;

namespace HonestTimeTracker.Desktop;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dbPath = GetDbPath();
        EnsureDbDirectory(dbPath);

        var services = new ServiceCollection();
        services.AddInfrastructure(dbPath);
        services.AddSingleton<MainWindow>();
        services.AddTransient<ProjectsViewModel>();
        services.AddTransient<ProjectsPage>();
        services.AddTransient<TasksViewModel>();
        services.AddTransient<TasksPage>();

        Services = services.BuildServiceProvider();

        await MigrateDatabase();
        await HandleOpenRecords();

        Services.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (Services is IDisposable d) d.Dispose();
        base.OnExit(e);
    }

    private static string GetDbPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "HonestTimeTracker", "htt.db");
    }

    private static void EnsureDbDirectory(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath)!;
        Directory.CreateDirectory(dir);
    }

    private static async Task MigrateDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    private static async Task HandleOpenRecords()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var openRecords = await db.Records
            .Where(r => r.FinishedAt == null)
            .ToListAsync();

        if (openRecords.Count == 0) return;

        var result = MessageBox.Show(
            $"Found {openRecords.Count} unfinished work record(s) — the application may have closed unexpectedly.\n\n" +
            "Yes — finish records now (FinishedAt = now)\n" +
            "No — delete records",
            "Unfinished records",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var now = DateTime.Now;
            foreach (var record in openRecords)
            {
                record.FinishedAt = now;
                record.MinutesSpent = (int)(now - record.StartedAt).TotalMinutes;
            }
        }
        else
        {
            db.Records.RemoveRange(openRecords);
        }

        await db.SaveChangesAsync();
    }
}
