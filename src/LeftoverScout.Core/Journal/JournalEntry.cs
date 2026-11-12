namespace LeftoverScout.Core.Journal;

/// <summary>One journal file as returned by <see cref="IJournalReader"/>.</summary>
/// <param name="FileName">The file name (no path).</param>
/// <param name="Status">How it is shown.</param>
/// <param name="Document">The content; null when the file is unreadable.</param>
/// <param name="SortTime">The start time (local clock time), used to sort newest first.</param>
public sealed record JournalEntry(string FileName, JournalEntryStatus Status, JournalDocument? Document, DateTime SortTime);
