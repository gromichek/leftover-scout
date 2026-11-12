using LeftoverScout.Core.Cleanup;

namespace LeftoverScout.Core.Journal;

/// <summary>The content of one journal file (schema version 1). Contains real paths: never log it.</summary>
public sealed class JournalDocument
{
    /// <summary>The only schema version this app writes and reads.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Schema version; 0 when the file has none.</summary>
    public int SchemaVersion { get; set; }

    /// <summary>Identifier of the session.</summary>
    public Guid Id { get; set; }

    /// <summary>Version of the app that wrote the file.</summary>
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>When the session began (ISO 8601 with offset).</summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>When the session ended; null while it is in progress.</summary>
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>The state of the session.</summary>
    public JournalState State { get; set; }

    /// <summary>Recycle Bin or permanent.</summary>
    public CleanupMode Mode { get; set; }

    /// <summary>The profile root the items belong to.</summary>
    public string ProfileRoot { get; set; } = string.Empty;

    /// <summary>Totals over the items.</summary>
    public JournalTotals Totals { get; set; } = new();

    /// <summary>The items, in processing order.</summary>
    public List<JournalItem> Items { get; set; } = [];
}
