using HonestTimeTracker.Desktop.Features.Projects;
using HonestTimeTracker.Desktop.Features.Records;
using HonestTimeTracker.Desktop.Features.Settings;
using HonestTimeTracker.Desktop.Features.Tasks;
using HonestTimeTracker.Desktop.Features.Today;
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
        services.AddTransient<RecordsViewModel>();
        services.AddTransient<RecordsPage>();
        services.AddTransient<TodayViewModel>();
        services.AddTransient<TodayPage>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsPage>();

        Services = services.BuildServiceProvider();

        await MigrateDatabase();

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
}
