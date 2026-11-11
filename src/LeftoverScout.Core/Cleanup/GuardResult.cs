namespace LeftoverScout.Core.Cleanup;

/// <summary>The answer of the guard for one folder: allowed, or blocked with reasons.</summary>
public sealed class GuardResult
{
    /// <summary>Creates a result. Any block reason makes the folder not allowed.</summary>
    public GuardResult(IReadOnlyList<BlockReason> blockReasons, IReadOnlyList<GuardWarning> warnings)
    {
        ArgumentNullException.ThrowIfNull(blockReasons);
        ArgumentNullException.ThrowIfNull(warnings);
        BlockReasons = blockReasons;
        Warnings = warnings;
    }

    /// <summary>True when there is no block reason.</summary>
    public bool IsAllowed => BlockReasons.Count == 0;

    /// <summary>The reasons why the folder is blocked; empty when allowed.</summary>
    public IReadOnlyList<BlockReason> BlockReasons { get; }

    /// <summary>Warnings for the confirmation; they are only filled for allowed folders.</summary>
    public IReadOnlyList<GuardWarning> Warnings { get; }
}
