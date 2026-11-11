namespace LeftoverScout.Core.Cleanup;

/// <summary>The outcome of asking the file system about one path.</summary>
public enum PathProbeStatus
{
    /// <summary>The path exists and its attributes were read.</summary>
    Found,

    /// <summary>The path (or a parent) does not exist.</summary>
    NotFound,

    /// <summary>The attributes could not be read (access denied or another error).</summary>
    Failed,
}
