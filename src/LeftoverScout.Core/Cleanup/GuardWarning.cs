namespace LeftoverScout.Core.Cleanup;

/// <summary>A warning about an allowed folder. A warning never allows a blocked folder.</summary>
/// <param name="Code">The machine-readable warning.</param>
/// <param name="Text">A short English explanation.</param>
public sealed record GuardWarning(GuardWarningCode Code, string Text)
{
    /// <summary>Creates the warning with the standard text of <paramref name="code"/>.</summary>
    public static GuardWarning Of(GuardWarningCode code) => new(code, code switch
    {
        GuardWarningCode.ChangedSinceScan => "The folder has changed since the scan.",
        GuardWarningCode.LowConfidence => "Low confidence: the folder is probably still used.",
        _ => "Medium confidence: check the folder before removing it.",
    });
}
