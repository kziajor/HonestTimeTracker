using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Settings;
using HonestTimeTracker.Desktop.Features.FloatingTimer;
using HonestTimeTracker.Desktop.Features.Leaves;
using HonestTimeTracker.Desktop.Features.Reports;
using HonestTimeTracker.Desktop.Features.Projects;
using HonestTimeTracker.Desktop.Features.Records;
using HonestTimeTracker.Desktop.Features.Settings;
using HonestTimeTracker.Desktop.Features.Tasks;
using HonestTimeTracker.Desktop.Features.Today;
using HonestTimeTracker.Desktop.Services;
using HonestTimeTracker.Infrastructure;
using HonestTimeTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace HonestTimeTracker.Desktop;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = AppLogging.CreateLogger();
        RegisterGlobalExceptionHandlers();
        Log.Information("Application starting");

        var dbPath = GetDbPath();
        EnsureDbDirectory(dbPath);

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug).AddSerilog(dispose: false));
        services.AddInfrastructure(dbPath);
        services.AddSingleton<ITimerStateService, TimerStateService>();
        services.AddSingleton<ITimerStopService, TimerStopService>();
        services.AddSingleton<ITimerSwitchService, TimerSwitchService>();
        services.AddSingleton<FloatingTimerViewModel>();
        services.AddSingleton<FloatingTimerWindow>();
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
        services.AddTransient<LeaveViewModel>();
        services.AddTransient<LeavePage>();
        services.AddSingleton<ReportsViewModel>();
        services.AddSingleton<ReportsPage>();

        Services = services.BuildServiceProvider();

        await MigrateDatabase();
        await ApplyLoggingSettings();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();

        try
        {
            var floatingWindow = Services.GetRequiredService<FloatingTimerWindow>();
            await floatingWindow.InitializeAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Floating timer initialization failed");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (Services is IDisposable d) d.Dispose();
        Log.Information("Application exiting");
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Fatal(args.ExceptionObject as Exception, "Unhandled exception (terminating: {IsTerminating})", args.IsTerminating);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI exception");
        MessageBox.Show($"An unexpected error occurred:\n{e.Exception.Message}\n\nDetails were written to the log.",
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static async Task ApplyLoggingSettings()
    {
        using var scope = Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetSettingsQuery, SettingsDto>>();
        var settings = await handler.HandleAsync(new GetSettingsQuery());
        AppLogging.SetVerboseTfsLogging(settings.VerboseTfsLogging);
    }

    private static string GetDbPath()
    {
        if (Environment.GetEnvironmentVariable("HTT_TEST_DB") is { } testDb) return testDb; // TEMP
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
