namespace LeftoverScout.Core.Classification;

/// <summary>How sure leftover detection is that a folder is a leftover (decision D-046).</summary>
public enum LeftoverConfidence
{
    /// <summary>Probably still used or changed recently; look carefully before removing.</summary>
    Low,

    /// <summary>Check the folder first.</summary>
    Medium,

    /// <summary>Very likely a leftover.</summary>
    High,
}
