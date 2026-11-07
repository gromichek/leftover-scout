namespace LeftoverScout.Windows;

/// <summary>
/// Information about the Windows versions the application supports.
/// </summary>
public static class WindowsPlatform
{
    /// <summary>
    /// Minimum supported Windows build: Windows 10 version 22H2 (build 19045).
    /// </summary>
    public const int MinimumBuild = 19045;

    /// <summary>
    /// Returns true when the current operating system is a supported Windows version.
    /// </summary>
    public static bool IsSupportedOs() => OperatingSystem.IsWindowsVersionAtLeast(10, 0, MinimumBuild);
}
