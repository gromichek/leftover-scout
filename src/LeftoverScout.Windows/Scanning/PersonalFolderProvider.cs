using System.Runtime.InteropServices;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Windows.Interop;

namespace LeftoverScout.Windows.Scanning;

/// <summary>
/// Finds the personal folders (D-020): the known folders Documents, Desktop, Pictures, Videos, Music and
/// Downloads (SHGetKnownFolderPath) plus the OneDrive roots from the environment.
/// </summary>
public sealed class PersonalFolderProvider : IPersonalFolderProvider
{
    // Known folder IDs from "KNOWNFOLDERID", Microsoft Learn.
    private static readonly Guid[] KnownFolderIds =
    [
        new("FDD39AD0-238F-46AF-ADB4-6C85480369C7"), // Documents
        new("B4BFCC3A-DB2C-424C-B029-7FE99A87C641"), // Desktop
        new("33E28130-4E1E-4676-835A-98395C3BC3BB"), // Pictures
        new("18989B1D-99B5-455B-841C-AB7C74E4DDFC"), // Videos
        new("4BD8D571-6D19-48D3-BE97-422220080E43"), // Music
        new("374DE290-123F-4565-9164-39C4925E467B"), // Downloads
    ];

    private static readonly string[] OneDriveVariables = ["OneDrive", "OneDriveConsumer", "OneDriveCommercial"];

    private readonly Func<string, string?> _getEnvironmentVariable;
    private readonly Func<IEnumerable<string>> _getKnownFolders;

    /// <summary>Creates a provider that reads the real known folders and environment.</summary>
    public PersonalFolderProvider()
        : this(Environment.GetEnvironmentVariable, GetKnownFolderPaths)
    {
    }

    /// <summary>Creates a provider with replaceable sources (for tests).</summary>
    internal PersonalFolderProvider(Func<string, string?> getEnvironmentVariable, Func<IEnumerable<string>> getKnownFolders)
    {
        _getEnvironmentVariable = getEnvironmentVariable;
        _getKnownFolders = getKnownFolders;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetPersonalFolderPaths()
    {
        var paths = new List<string>(_getKnownFolders());

        foreach (string variable in OneDriveVariables)
        {
            string? value = _getEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(value))
            {
                paths.Add(value);
            }
        }

        return paths;
    }

    private static List<string> GetKnownFolderPaths()
    {
        var paths = new List<string>();
        foreach (Guid id in KnownFolderIds)
        {
            int result = NativeMethods.SHGetKnownFolderPath(in id, 0, 0, out nint pointer);
            try
            {
                if (result >= 0 && pointer != 0)
                {
                    string? path = Marshal.PtrToStringUni(pointer);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        paths.Add(path);
                    }
                }
            }
            finally
            {
                if (pointer != 0)
                {
                    Marshal.FreeCoTaskMem(pointer);
                }
            }
        }

        return paths;
    }
}
