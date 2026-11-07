using System.Reflection;

namespace LeftoverScout.App.Startup;

/// <summary>
/// Version information of the running application.
/// </summary>
public static class AppInfo
{
    /// <summary>
    /// Returns the informational version (for example "0.1.0"), without the build metadata suffix
    /// that the .NET SDK appends after a plus sign.
    /// </summary>
    public static string GetVersion()
    {
        string? version = typeof(AppInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(version))
        {
            return "unknown";
        }

        int plus = version.IndexOf('+', StringComparison.Ordinal);
        return plus >= 0 ? version[..plus] : version;
    }
}
