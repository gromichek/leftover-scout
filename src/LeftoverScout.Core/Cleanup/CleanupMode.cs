namespace LeftoverScout.Core.Cleanup;

/// <summary>How the selected folders are removed.</summary>
public enum CleanupMode
{
    /// <summary>Move to the Recycle Bin. Never falls back to permanent deletion (decisions D-004, D-057, D-062).</summary>
    RecycleBin,

    /// <summary>Delete permanently. Only for the explicit option with double confirmation (D-004).</summary>
    Permanent,
}
