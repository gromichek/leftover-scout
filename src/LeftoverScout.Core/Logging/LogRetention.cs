using System.Text.RegularExpressions;

namespace LeftoverScout.Core.Logging;

/// <summary>
/// Keeps the log folder small by removing the oldest log files written by this application.
/// </summary>
public static partial class LogRetention
{
    /// <summary>Number of log files that are kept.</summary>
    public const int DefaultFilesToKeep = 7;

    /// <summary>Returns the file name used for the log of the given day.</summary>
    public static string GetFileName(DateTimeOffset date) => $"leftoverscout-{date:yyyyMMdd}.log";

    /// <summary>
    /// Deletes all but the newest <paramref name="filesToKeep"/> log files. Only files whose name matches
    /// <c>leftoverscout-yyyyMMdd.log</c> are considered; all other files are left alone. Never throws.
    /// </summary>
    /// <param name="logsFolder">The application's own log folder.</param>
    /// <param name="filesToKeep">How many of the newest files to keep.</param>
    public static void Apply(string logsFolder, int filesToKeep = DefaultFilesToKeep)
    {
        try
        {
            if (!Directory.Exists(logsFolder))
            {
                return;
            }

            // The date is part of the name, so ordering by name orders by day.
            List<string> logFiles = Directory.EnumerateFiles(logsFolder)
                .Where(path => LogFileNamePattern().IsMatch(Path.GetFileName(path)))
                .OrderByDescending(path => Path.GetFileName(path), StringComparer.Ordinal)
                .ToList();

            foreach (string oldFile in logFiles.Skip(filesToKeep))
            {
                try
                {
                    // The application's own log file, not user data.
                    File.Delete(oldFile);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // An old log that cannot be removed is not worth failing for.
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Logging housekeeping must never break the app.
        }
    }

    [GeneratedRegex(@"^leftoverscout-\d{8}\.log$", RegexOptions.CultureInvariant)]
    private static partial Regex LogFileNamePattern();
}
