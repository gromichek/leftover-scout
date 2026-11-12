using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Journal;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Classification;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeftoverScout.Core.Tests.Journal;

/// <summary>An in-memory journal folder; can be told to fail every write.</summary>
internal sealed class InMemoryJournalStorage : IJournalStorage
{
    public Dictionary<string, string> Files { get; } = new(StringComparer.Ordinal);

    public bool FailWrites { get; set; }

    public bool FailReads { get; set; }

    public int Writes { get; private set; }

    public string FolderPath => @"C:\Apps\SampleApp\data\journal";

    public bool EnsureFolder() => true;

    public void WriteAtomic(string fileName, string content)
    {
        if (FailWrites)
        {
            throw new IOException("Simulated write failure.");
        }

        Writes++;
        Files[fileName] = content;
    }

    public IReadOnlyList<string> ListFileNames() => [.. Files.Keys];

    public string ReadAllText(string fileName) =>
        FailReads ? throw new IOException("Simulated read failure.") : Files[fileName];
}

internal static class JournalTestKit
{
    public const string Root = @"C:\Users\SampleUser";

    public static readonly DateTimeOffset Now = new(2026, 11, 12, 10, 30, 0, TimeSpan.Zero);

    public static CleanupItem Item(string name, long size = 1000) =>
        new(Root + @"\AppData\Roaming\" + name, FindingKind.Leftover, LeftoverConfidence.High, size, null);

    public static CleanupRequest Request(params CleanupItem[] items) =>
        new(new ScanTarget(Root, isCurrentUser: false), items);

    public static CleanupItemResult Removed(CleanupItem item) =>
        new(item, CleanupOutcome.Removed, item.SizeBytes);

    public static CleanupJournal NewJournal(IJournalStorage storage, ActiveJournalSessions? active = null) =>
        new(storage, active ?? new ActiveJournalSessions(), new FixedTimeProvider(Now), "0.1.0-test", NullLogger<CleanupJournal>.Instance);

    public static JournalDocument Parse(InMemoryJournalStorage storage, JournalHandle handle) =>
        JournalJson.TryParse(storage.Files.Single(pair => pair.Key.Contains(handle.Id.ToString("N")[..8], StringComparison.Ordinal)).Value)!;
}
