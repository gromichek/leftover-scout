namespace LeftoverScout.Core.Scanning;

/// <summary>
/// Describes which user profile a scan inspects.
/// </summary>
/// <remarks>
/// Phase 1 only supports the profile of the current user (see decision D-003 in docs/LEAD_HANDOVER.md).
/// Later phases add other profiles (administrator mode); all scanning code must take a
/// <see cref="ScanTarget"/> instead of reading the current user's profile path directly.
/// </remarks>
public sealed record ScanTarget
{
    /// <summary>
    /// Creates a scan target for the given profile root folder.
    /// </summary>
    /// <param name="profileRootPath">Fully qualified path of the profile root folder.</param>
    /// <param name="isCurrentUser">True when the profile belongs to the user running the application.</param>
    /// <exception cref="ArgumentException">The path is empty or not fully qualified.</exception>
    public ScanTarget(string profileRootPath, bool isCurrentUser)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileRootPath);

        if (!Path.IsPathFullyQualified(profileRootPath))
        {
            throw new ArgumentException("The profile root path must be fully qualified.", nameof(profileRootPath));
        }

        ProfileRootPath = Path.TrimEndingDirectorySeparator(profileRootPath);
        IsCurrentUser = isCurrentUser;
    }

    /// <summary>
    /// Fully qualified path of the profile root folder, without a trailing separator.
    /// </summary>
    public string ProfileRootPath { get; }

    /// <summary>
    /// True when the profile belongs to the user running the application.
    /// </summary>
    public bool IsCurrentUser { get; }

    /// <summary>
    /// Creates a scan target for the profile of the user running the application.
    /// </summary>
    public static ScanTarget ForCurrentUser() =>
        new(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), isCurrentUser: true);
}
