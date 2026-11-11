namespace LeftoverScout.Core.Cleanup;

/// <summary>One reason why a folder must not be deleted.</summary>
/// <param name="Code">The machine-readable reason.</param>
/// <param name="Text">A short English explanation.</param>
public sealed record BlockReason(BlockReasonCode Code, string Text)
{
    /// <summary>Creates the reason with the standard text of <paramref name="code"/>, plus an optional detail.</summary>
    public static BlockReason Of(BlockReasonCode code, string? detail = null)
    {
        string text = code switch
        {
            BlockReasonCode.InvalidPath => "The path is not a plain, fully qualified folder path.",
            BlockReasonCode.DevicePath => "The path is a device or extended-length path, which is not accepted.",
            BlockReasonCode.NotInsideProfile => "The folder is not inside the profile folder.",
            BlockReasonCode.FixedRoot => "This is the profile folder or a fixed folder of the profile (AppData, Roaming, Local, LocalLow).",
            BlockReasonCode.DenyList => "The folder is on the built-in list of protected Windows folders.",
            BlockReasonCode.ProtectedByRule => "The folder is protected by a rule.",
            BlockReasonCode.AppFolder => "The folder belongs to LeftoverScout itself (its program or data folder).",
            BlockReasonCode.PersonalFolder => "The folder is, lies inside, or contains a personal folder.",
            BlockReasonCode.NotFound => "The folder does not exist any more.",
            BlockReasonCode.NotADirectory => "The path is not a folder.",
            BlockReasonCode.ReparsePoint => "The folder is a junction or symbolic link.",
            BlockReasonCode.ReparsePointInPath => "A parent folder on the way to it is a junction or symbolic link.",
            BlockReasonCode.CloudNotPopulated => "The folder or a parent is an online-only cloud folder that is not downloaded.",
            _ => "The folder could not be checked.",
        };

        return new BlockReason(code, detail is null ? text : $"{text} ({detail})");
    }
}
