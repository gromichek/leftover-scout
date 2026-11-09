namespace LeftoverScout.Core.Rules;

/// <summary>The kind of a rule (decision D-041).</summary>
public enum RuleKind
{
    /// <summary>The folder belongs to a program.</summary>
    Ownership,

    /// <summary>The folder holds a cache, temporary files, logs or crash dumps.</summary>
    Cache,

    /// <summary>The folder must never be offered for deletion.</summary>
    Protected,
}
