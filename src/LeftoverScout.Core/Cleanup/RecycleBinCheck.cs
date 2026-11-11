namespace LeftoverScout.Core.Cleanup;

/// <summary>The answer of the Recycle Bin pre-check (D-062 layer 1).</summary>
public enum RecycleBinCheck
{
    /// <summary>Nothing speaks against the Recycle Bin.</summary>
    Allowed,

    /// <summary>The bin is turned off for the drive.</summary>
    TurnedOff,

    /// <summary>The item is larger than the maximum size of the bin.</summary>
    TooLarge,
}
