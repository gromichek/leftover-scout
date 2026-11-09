namespace LeftoverScout.Core.Findings;

/// <summary>What a finding is (decision D-053).</summary>
public enum FindingKind
{
    /// <summary>Data that probably belongs to a program that is no longer installed.</summary>
    Leftover,

    /// <summary>A program cache that is rebuilt when needed.</summary>
    Cache,

    /// <summary>Temporary files.</summary>
    Temp,

    /// <summary>Log files.</summary>
    Logs,

    /// <summary>Crash dumps and error reports.</summary>
    CrashDumps,
}
