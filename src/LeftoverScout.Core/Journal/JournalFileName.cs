using System.Globalization;

namespace LeftoverScout.Core.Journal;

/// <summary>The journal file name format: <c>cleanup-&lt;yyyyMMdd-HHmmss&gt;-&lt;short id&gt;.json</c>.</summary>
public static class JournalFileName
{
    /// <summary>Search pattern that matches journal files.</summary>
    public const string SearchPattern = "cleanup-*.json";

    /// <summary>Builds the file name for a session.</summary>
    /// <param name="startedAt">When the session began (the time in its own offset is used).</param>
    /// <param name="id">The session id; its first eight hex digits are the short id.</param>
    public static string Create(DateTimeOffset startedAt, Guid id) =>
        string.Create(CultureInfo.InvariantCulture, $"cleanup-{startedAt:yyyyMMdd-HHmmss}-{id.ToString("N")[..8]}.json");

    /// <summary>Reads the start time from a file name; null when the name does not follow the format.</summary>
    public static DateTime? TryParseTime(string fileName)
    {
        const string prefix = "cleanup-";
        const int stampLength = 15;
        if (!fileName.StartsWith(prefix, StringComparison.Ordinal) || fileName.Length < prefix.Length + stampLength)
        {
            return null;
        }

        return DateTime.TryParseExact(
            fileName.AsSpan(prefix.Length, stampLength),
            "yyyyMMdd-HHmmss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime time)
            ? time
            : null;
    }
}
