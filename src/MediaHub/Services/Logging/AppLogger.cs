using Microsoft.Maui.Storage;

namespace MediaHub.Services.Logging;

/// <summary>
/// Minimal thread-safe file logger used by the global exception handlers.
/// Appends each entry to a single log file and never throws, so logging can
/// never take the application down or leak into user-facing flows.
/// </summary>
public static class AppLogger
{
    private static readonly object Sync = new();
    private static string? _logPath;

    /// <summary>Resolved path of the log file, e.g. .../logs/error.log.</summary>
    public static string LogFilePath => _logPath ??= ResolvePath();

    /// <summary>
    /// Writes the exception (type, message, stack trace) and a timestamp to the
    /// log file, creating the folder on demand. Exceptions here are swallowed.
    /// </summary>
    public static void Log(Exception exception)
    {
        if (exception is null)
            return;

        try
        {
            var entry =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exception.GetType().FullName}: {exception.Message}" +
                Environment.NewLine +
                exception.StackTrace +
                Environment.NewLine +
                Environment.NewLine;

            lock (Sync)
            {
                var directory = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.AppendAllText(LogFilePath, entry);
            }
        }
        catch
        {
            // Logging must never crash the process.
        }
    }

    private static string ResolvePath()
    {
        return Path.Combine(FileSystem.AppDataDirectory, "logs", "error.log");
    }
}
