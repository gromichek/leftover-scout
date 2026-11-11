namespace LeftoverScout.Core.Cleanup;

/// <summary>Why the guard blocks a folder.</summary>
public enum BlockReasonCode
{
    /// <summary>The path is empty, relative, has "." or ".." segments, wildcards, invalid characters or trailing dots or spaces.</summary>
    InvalidPath,

    /// <summary>The path is a device or extended-length path (<c>\\?\</c>, <c>\\.\</c>, reserved device names).</summary>
    DevicePath,

    /// <summary>The path is not strictly inside the profile folder.</summary>
    NotInsideProfile,

    /// <summary>The path is the profile folder or one of AppData, Roaming, Local, LocalLow.</summary>
    FixedRoot,

    /// <summary>The path is on the hard-coded deny list (Windows security folders).</summary>
    DenyList,

    /// <summary>The path is protected by a rule of the rule files.</summary>
    ProtectedByRule,

    /// <summary>The path is the program or data folder of LeftoverScout, or lies inside or contains one.</summary>
    AppFolder,

    /// <summary>The path is, lies inside, or contains a personal folder.</summary>
    PersonalFolder,

    /// <summary>The folder does not exist.</summary>
    NotFound,

    /// <summary>The path exists but is not a folder.</summary>
    NotADirectory,

    /// <summary>The folder itself is a junction or symbolic link.</summary>
    ReparsePoint,

    /// <summary>A parent folder between the profile and the item is a junction or symbolic link.</summary>
    ReparsePointInPath,

    /// <summary>The folder or a parent is a cloud folder that is not downloaded.</summary>
    CloudNotPopulated,

    /// <summary>A live check failed, so the folder cannot be shown to be safe.</summary>
    CouldNotBeChecked,
}
