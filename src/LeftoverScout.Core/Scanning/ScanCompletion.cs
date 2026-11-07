namespace LeftoverScout.Core.Scanning;

/// <summary>
/// How a scan ended.
/// </summary>
public enum ScanCompletion
{
    /// <summary>The whole profile was walked.</summary>
    Completed,

    /// <summary>The user cancelled; there are no totals.</summary>
    Cancelled,
}
