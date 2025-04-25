using Serilog;
using Serilog.Events;

namespace Serilogs.Main;

public class SerilogHelper
{
    public static ILogger GetLogger(
        string logDirectoryName = "logs",
        int retailedFileCountLimit = 45,
        bool useConsole = true,
        bool useTextFileLogs = true,
        LogEventLevel eventLevel = LogEventLevel.Information,
        string outputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {CorrelationId} {Level:u3} {Username} {Message:lj}{Exception}{NewLine}"
    )
    {
        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logDirectoryName);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var filePath = Path.Combine(folderPath, "log.txt");

        var loggerconfiguration = new LoggerConfiguration();
        if (useTextFileLogs)
        {
            loggerconfiguration = loggerconfiguration.WriteTo.File(
                path: filePath,
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: eventLevel,
                retainedFileCountLimit: retailedFileCountLimit,
                outputTemplate: outputTemplate
            );
        }

        if (useConsole)
        {
            loggerconfiguration = loggerconfiguration.WriteTo.Console(
                restrictedToMinimumLevel: eventLevel,
                outputTemplate: outputTemplate
            );
        }
        Log.Logger = loggerconfiguration.CreateLogger();

        return Log.Logger;
    }

    public static ILogger GetLogger(SerilogSettings settings)
    {
        return GetLogger(
            logDirectoryName: settings.LogFolderName ?? throw new NullReferenceException(),
            retailedFileCountLimit: settings.FileCountLimit ?? throw new NullReferenceException(),
            useConsole: settings.LogToConsole ?? throw new NullReferenceException(),
            useTextFileLogs: settings.LogToFile ?? throw new NullReferenceException(),
            eventLevel: Enum.Parse<LogEventLevel>(
                settings.LogEventLevel ?? throw new NullReferenceException()
            ),
            outputTemplate: settings.OutputTemplate ?? throw new NullReferenceException()
        );
    }
}

public class SerilogSettings
{
    public string? LogFolderName { get; set; }
    public bool? LogToFile { get; set; }
    public bool? LogToConsole { get; set; }
    public string? LogEventLevel { get; set; }
    public string? OutputTemplate { get; set; }
    public int? FileCountLimit { get; set; }
}
