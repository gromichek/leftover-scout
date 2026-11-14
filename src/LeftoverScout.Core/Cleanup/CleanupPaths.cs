namespace LeftoverScout.Core.Cleanup;

/// <summary>Path text helpers for the cleanup pages. Pure.</summary>
public static class CleanupPaths
{
    /// <summary>The path below the profile root; the full path when it is not inside the root.</summary>
    public static string Relative(string profileRoot, string fullPath)
    {
        ArgumentNullException.ThrowIfNull(profileRoot);
        ArgumentNullException.ThrowIfNull(fullPath);
        return CleanupService.RelativePath(profileRoot, fullPath);
    }
}
