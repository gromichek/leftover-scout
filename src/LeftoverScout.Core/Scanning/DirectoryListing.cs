namespace LeftoverScout.Core.Scanning;

/// <summary>
/// The result of listing one folder. Failures are data, never exceptions.
/// </summary>
/// <param name="Entries">Entries that could be read (possibly partial when an error occurred).</param>
/// <param name="ErrorReason">Short reason when the listing failed or stopped early; null on success.</param>
/// <param name="IsAccessDenied">True when the error was "access denied".</param>
public sealed record DirectoryListing(IReadOnlyList<DirectoryEntry> Entries, string? ErrorReason = null, bool IsAccessDenied = false)
{
    /// <summary>Creates a successful listing.</summary>
    public static DirectoryListing Success(IReadOnlyList<DirectoryEntry> entries) => new(entries);

    /// <summary>Creates a failed listing without entries.</summary>
    public static DirectoryListing Failure(string reason, bool isAccessDenied = false) => new([], reason, isAccessDenied);
}
