using LeftoverScout.App.ViewModels;
using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Formatting;
using LeftoverScout.Core.Help;
using LeftoverScout.Core.Journal;
using LeftoverScout.Core.Shell;

namespace LeftoverScout.App.Tests;

public class HistoryPageViewModelTests
{
    private static readonly DateTimeOffset Started = new(2026, 11, 12, 10, 30, 0, TimeSpan.Zero);

    private sealed class FakeReader : IJournalReader
    {
        public List<JournalEntry> Entries { get; } = [];

        public IReadOnlyList<JournalEntry> ReadAll() => [.. Entries.OrderByDescending(entry => entry.SortTime)];
    }

    private sealed class FakeJournal : ICleanupJournal
    {
        public event EventHandler? SessionRecorded;

        public void Raise() => SessionRecorded?.Invoke(this, EventArgs.Empty);

        public JournalHandle Begin(CleanupRequest request, CleanupMode mode) => throw new NotSupportedException();

        public void Record(JournalHandle handle, CleanupItemResult result) => throw new NotSupportedException();

        public void Complete(JournalHandle handle, CleanupSessionResult session) => throw new NotSupportedException();

        public void Abort(JournalHandle handle, JournalState state) => throw new NotSupportedException();
    }

    private sealed class FakeStorage : IJournalStorage
    {
        public bool CanCreate { get; set; } = true;

        public int EnsureCalls { get; private set; }

        public string FolderPath => @"C:\Apps\SampleApp\data\journal";

        public bool EnsureFolder()
        {
            EnsureCalls++;
            return CanCreate;
        }

        public void WriteAtomic(string fileName, string content) => throw new NotSupportedException();

        public IReadOnlyList<string> ListFileNames() => [];

        public string ReadAllText(string fileName) => throw new NotSupportedException();
    }

    private sealed class RecordingShell : IShellLauncher
    {
        public int RecycleBinOpened { get; private set; }

        public List<string> OpenedFolders { get; } = [];

        public void OpenFolder(string folderPath) => OpenedFolders.Add(folderPath);

        public void SelectFile(string filePath)
        {
        }

        public void OpenRecycleBin() => RecycleBinOpened++;

        public void OpenUrl(string url)
        {
        }
    }

    private sealed class FakeClipboard : IClipboardService
    {
        public List<string> Texts { get; } = [];

        public bool TrySetText(string text)
        {
            Texts.Add(text);
            return true;
        }
    }

    private static JournalItem Item(string name, JournalOutcome outcome, long size = 2048, bool unexpected = false) => new()
    {
        FullPath = @"C:\Users\SampleUser\AppData\Roaming\" + name,
        RelativePath = @"AppData\Roaming\" + name,
        Kind = FindingKind.Leftover,
        Confidence = LeftoverConfidence.High,
        SizeAtScan = size,
        Outcome = outcome,
        Reason = outcome == JournalOutcome.Removed ? null : "A reason.",
        BytesRemoved = outcome == JournalOutcome.Removed ? size : 0,
        DeletedPermanentlyUnexpectedly = unexpected,
    };

    private static JournalEntry Entry(DateTimeOffset started, JournalEntryStatus status, params JournalItem[] items)
    {
        var document = new JournalDocument
        {
            SchemaVersion = 1,
            Id = Guid.NewGuid(),
            StartedAt = started,
            Mode = CleanupMode.RecycleBin,
            State = JournalState.Completed,
            Totals = new JournalTotals { ItemCount = items.Length, BytesRemoved = items.Sum(item => item.BytesRemoved) },
            Items = [.. items],
        };
        return new JournalEntry(JournalFileName.Create(started, document.Id), status, document, started.DateTime);
    }

    private static (HistoryPageViewModel Page, FakeReader Reader, FakeJournal Journal, FakeStorage Storage, RecordingShell Shell, FakeClipboard Clipboard) Create()
    {
        var reader = new FakeReader();
        var journal = new FakeJournal();
        var storage = new FakeStorage();
        var shell = new RecordingShell();
        var clipboard = new FakeClipboard();
        return (new HistoryPageViewModel(reader, journal, storage, shell, clipboard), reader, journal, storage, shell, clipboard);
    }

    [Fact]
    public async Task EmptyState_ShowsNoCleanupsYet()
    {
        (HistoryPageViewModel page, _, _, _, _, _) = Create();

        await page.RefreshAsync();

        Assert.True(page.HasNoSessions);
        Assert.False(page.HasSessions);
        Assert.Equal("No cleanups yet.", page.EmptyTitle);
        Assert.False(string.IsNullOrWhiteSpace(page.EmptyDescription));
        Assert.Equal(HelpTopics.History, page.HelpTopicId);
        Assert.Equal("History", page.Title);
    }

    [Fact]
    public async Task Sessions_ShowTextsNewestFirst_AndSelectNewest()
    {
        (HistoryPageViewModel page, FakeReader reader, _, _, _, _) = Create();
        reader.Entries.Add(Entry(Started.AddDays(-1), JournalEntryStatus.Completed, Item("Old", JournalOutcome.Removed)));
        reader.Entries.Add(Entry(
            Started,
            JournalEntryStatus.Completed,
            Item("A", JournalOutcome.Removed, 1024),
            Item("B", JournalOutcome.PartlyRemoved),
            Item("C", JournalOutcome.SkippedRecycleBinOff)));

        await page.RefreshAsync();

        Assert.Equal(2, page.Sessions.Count);
        HistorySessionRowViewModel newest = page.Sessions[0];
        Assert.Same(newest, page.SelectedSession);
        Assert.Equal("Recycle Bin", newest.ModeText);
        Assert.Equal("3", newest.ItemsText);
        Assert.Equal(SizeFormatter.Format(1024), newest.RemovedText);
        Assert.Equal("1 removed · 1 partly · 1 skipped", newest.ResultText);
        Assert.True(newest.SortTime > page.Sessions[1].SortTime);
    }

    [Fact]
    public async Task Details_ListSelectedSessionItems()
    {
        (HistoryPageViewModel page, FakeReader reader, _, _, _, _) = Create();
        reader.Entries.Add(Entry(Started, JournalEntryStatus.Completed, Item("A", JournalOutcome.Removed), Item("B", JournalOutcome.Blocked)));

        await page.RefreshAsync();

        Assert.Equal(2, page.Details.Count);
        HistoryItemRowViewModel first = page.Details[0];
        Assert.Equal(@"AppData\Roaming\A", first.RelativePath);
        Assert.Equal("Leftover", first.KindText);
        Assert.Equal("High", first.ConfidenceText);
        Assert.Equal(SizeFormatter.Format(2048), first.SizeText);
        Assert.Equal("Removed", first.OutcomeText);
        Assert.Equal(string.Empty, first.ReasonText);
        Assert.Equal("Blocked", page.Details[1].OutcomeText);
        Assert.Equal("A reason.", page.Details[1].ReasonText);
        Assert.False(page.HasUnexpectedPermanentWarning);
    }

    [Fact]
    public async Task Details_UnexpectedPermanentDeletion_ShowsWarning()
    {
        (HistoryPageViewModel page, FakeReader reader, _, _, _, _) = Create();
        reader.Entries.Add(Entry(Started, JournalEntryStatus.Completed, Item("A", JournalOutcome.Removed, unexpected: true)));

        await page.RefreshAsync();

        Assert.True(page.HasUnexpectedPermanentWarning);
        Assert.Contains("permanently", page.UnexpectedPermanentWarning, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnreadableJournal_ShowsFileNameAndNoDetails()
    {
        (HistoryPageViewModel page, FakeReader reader, _, _, _, _) = Create();
        reader.Entries.Add(new JournalEntry("cleanup-20261112-103000-deadbeef.json", JournalEntryStatus.Unreadable, null, DateTime.MinValue));

        await page.RefreshAsync();

        Assert.Equal("Unreadable journal", page.Sessions[0].ResultText);
        Assert.Empty(page.Details);
        Assert.True(page.HasDetailsMessage);
        Assert.Contains("cleanup-20261112-103000-deadbeef.json", page.DetailsMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InterruptedJournal_ShowsInterruptedAndExplanation()
    {
        (HistoryPageViewModel page, FakeReader reader, _, _, _, _) = Create();
        reader.Entries.Add(Entry(Started, JournalEntryStatus.Interrupted, Item("A", JournalOutcome.Pending)));

        await page.RefreshAsync();

        Assert.Equal("Interrupted", page.Sessions[0].ResultText);
        Assert.Contains("interrupted", page.DetailsMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Pending", page.Details[0].OutcomeText);
    }

    [Fact]
    public async Task SessionRecordedEvent_RefreshesTheList()
    {
        (HistoryPageViewModel page, FakeReader reader, FakeJournal journal, _, _, _) = Create();
        await page.RefreshAsync();
        Assert.True(page.HasNoSessions);

        reader.Entries.Add(Entry(Started, JournalEntryStatus.Completed, Item("A", JournalOutcome.Removed)));
        journal.Raise();
        await page.RefreshAsync();

        Assert.True(page.HasSessions);
        Assert.Single(page.Sessions);
    }

    [Fact]
    public async Task Refresh_KeepsTheSelectedSession()
    {
        (HistoryPageViewModel page, FakeReader reader, _, _, _, _) = Create();
        reader.Entries.Add(Entry(Started.AddDays(-1), JournalEntryStatus.Completed, Item("Old", JournalOutcome.Removed)));
        await page.RefreshAsync();
        string selected = page.SelectedSession!.FileName;

        reader.Entries.Add(Entry(Started, JournalEntryStatus.Completed, Item("New", JournalOutcome.Removed)));
        await page.RefreshAsync();

        Assert.Equal(selected, page.SelectedSession!.FileName);
    }

    [Fact]
    public void OpenRecycleBinCommand_OpensTheRecycleBin()
    {
        (HistoryPageViewModel page, _, _, _, RecordingShell shell, _) = Create();

        page.OpenRecycleBinCommand.Execute(null);

        Assert.Equal(1, shell.RecycleBinOpened);
    }

    [Fact]
    public void OpenJournalFolderCommand_CreatesAndOpensTheFolder()
    {
        (HistoryPageViewModel page, _, _, FakeStorage storage, RecordingShell shell, _) = Create();

        page.OpenJournalFolderCommand.Execute(null);

        Assert.Equal(1, storage.EnsureCalls);
        Assert.Equal([@"C:\Apps\SampleApp\data\journal"], shell.OpenedFolders);
    }

    [Fact]
    public void OpenJournalFolderCommand_FolderCannotBeCreated_ShowsMessage()
    {
        (HistoryPageViewModel page, _, _, FakeStorage storage, RecordingShell shell, _) = Create();
        storage.CanCreate = false;

        page.OpenJournalFolderCommand.Execute(null);

        Assert.Empty(shell.OpenedFolders);
        Assert.NotEmpty(page.StatusMessage);
    }

    [Fact]
    public async Task CopyPathCommand_CopiesFullPathOfSelectedDetail()
    {
        (HistoryPageViewModel page, FakeReader reader, _, _, _, FakeClipboard clipboard) = Create();
        reader.Entries.Add(Entry(Started, JournalEntryStatus.Completed, Item("A", JournalOutcome.Removed)));
        await page.RefreshAsync();
        Assert.False(page.CopyPathCommand.CanExecute(null));

        page.SelectedDetail = page.Details[0];
        Assert.True(page.CopyPathCommand.CanExecute(null));
        page.CopyPathCommand.Execute(null);

        Assert.Equal([@"C:\Users\SampleUser\AppData\Roaming\A"], clipboard.Texts);
    }
}
