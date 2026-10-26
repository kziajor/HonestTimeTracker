using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.IO;

namespace HonestTimeTracker.Desktop.Services;

public static class AppLogging
{
    private static readonly LoggingLevelSwitch TfsLevel = new(LogEventLevel.Information);

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HonestTimeTracker", "logs");

    public static Serilog.ILogger CreateLogger() =>
        new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("HonestTimeTracker.Application.Tfs", TfsLevel)
            .MinimumLevel.Override("HonestTimeTracker.Infrastructure.Tfs", TfsLevel)
            .WriteTo.File(
                Path.Combine(LogDirectory, "htt-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

    /// Verbose mode logs full TFS requests and responses (never headers, so the PAT stays out of the log).
    public static void SetVerboseTfsLogging(bool verbose) =>
        TfsLevel.MinimumLevel = verbose ? LogEventLevel.Debug : LogEventLevel.Information;
}
