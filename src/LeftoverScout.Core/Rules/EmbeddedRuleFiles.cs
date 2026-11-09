using System.Reflection;
using System.Text;

namespace LeftoverScout.Core.Rules;

/// <summary>
/// Reads the rule files that are embedded in this assembly with logical names like <c>Rules/caches.json</c>
/// (the repository folder <c>rules/</c>, decision D-040).
/// </summary>
public static class EmbeddedRuleFiles
{
    private const string Prefix = "Rules/";

    /// <summary>Returns all embedded rule files, sorted by name.</summary>
    public static IReadOnlyList<RuleFile> Read() => Read(typeof(EmbeddedRuleFiles).Assembly);

    /// <summary>Returns all rule files embedded in <paramref name="assembly"/>, sorted by name.</summary>
    public static IReadOnlyList<RuleFile> Read(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var files = new List<RuleFile>();
        foreach (string resourceName in assembly.GetManifestResourceNames().Order(StringComparer.Ordinal))
        {
            string normalized = resourceName.Replace('\\', '/');
            if (!normalized.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) || !normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            files.Add(new RuleFile(normalized[Prefix.Length..], reader.ReadToEnd()));
        }

        return files;
    }
}
