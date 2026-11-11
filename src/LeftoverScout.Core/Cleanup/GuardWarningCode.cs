namespace LeftoverScout.Core.Cleanup;

/// <summary>Why the guard warns about a folder that is allowed.</summary>
public enum GuardWarningCode
{
    /// <summary>The folder has newer content than the scan saw.</summary>
    ChangedSinceScan,

    /// <summary>The confidence is Low: the folder is probably still used.</summary>
    LowConfidence,

    /// <summary>The confidence is Medium: check the folder first.</summary>
    MediumConfidence,
}
