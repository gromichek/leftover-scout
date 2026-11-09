namespace LeftoverScout.Core.Rules;

/// <summary>What kind of disposable data a cache rule describes.</summary>
public enum CacheCategory
{
    /// <summary>A program cache that is rebuilt when needed.</summary>
    Cache,

    /// <summary>Temporary files.</summary>
    Temp,

    /// <summary>Log files.</summary>
    Logs,

    /// <summary>Crash dumps and error reports.</summary>
    CrashDumps,
}
