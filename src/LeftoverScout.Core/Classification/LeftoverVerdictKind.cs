namespace LeftoverScout.Core.Classification;

/// <summary>What leftover detection concluded about a folder (decision D-046).</summary>
public enum LeftoverVerdictKind
{
    /// <summary>No decision could be made (generic name, unclear case). Shown without a label.</summary>
    Unknown,

    /// <summary>The folder belongs to an installed program.</summary>
    InUse,

    /// <summary>The folder probably belongs to a program that is no longer installed.</summary>
    Leftover,
}
