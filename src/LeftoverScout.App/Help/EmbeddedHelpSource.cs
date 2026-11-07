using System.Reflection;
using System.Text;
using LeftoverScout.Core.Help;

namespace LeftoverScout.App.Help;

/// <summary>
/// Reads help files that are embedded in the executable with logical names like <c>Help/en/scan.md</c>.
/// </summary>
public sealed class EmbeddedHelpSource : IHelpSource
{
    private const string Prefix = "Help/";

    private readonly Assembly _assembly;
    private readonly Dictionary<string, string> _resourceNames;

    /// <summary>Creates a source that reads from the application assembly.</summary>
    public EmbeddedHelpSource()
        : this(typeof(EmbeddedHelpSource).Assembly)
    {
    }

    /// <summary>Creates a source that reads from <paramref name="assembly"/>.</summary>
    public EmbeddedHelpSource(Assembly assembly)
    {
        _assembly = assembly;

        // MSBuild's %(RecursiveDir) uses backslashes on Windows; normalize so lookups always use "/".
        _resourceNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in assembly.GetManifestResourceNames())
        {
            string normalized = name.Replace('\\', '/');
            if (normalized.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                _resourceNames[normalized] = name;
            }
        }
    }

    /// <summary>Returns the normalized logical names of all embedded help files, for example <c>Help/en/scan.md</c>.</summary>
    public IReadOnlyCollection<string> GetFileNames() => _resourceNames.Keys;

    /// <inheritdoc />
    public string? TryRead(HelpLanguage language, string fileName)
    {
        string key = $"{Prefix}{HelpLanguages.ToCode(language)}/{fileName}";
        if (!_resourceNames.TryGetValue(key, out string? resourceName))
        {
            return null;
        }

        using System.IO.Stream? stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new System.IO.StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
