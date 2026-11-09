using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Scanning;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Rules;

internal static class RuleTestHelper
{
    /// <summary>Builds a rule file with the given rule JSON objects.</summary>
    public static RuleFile File(params string[] ruleJson) =>
        new("test.json", $$"""{ "schemaVersion": 1, "rules": [ {{string.Join(",", ruleJson)}} ] }""");

    public static RuleLoadResult Load(params string[] ruleJson) => RuleLoader.Load([File(ruleJson)]);

    public static string Protected(string id, string path, string scope) =>
        $$"""{ "id": "{{id}}", "kind": "protected", "paths": ["{{path.Replace("\\", "\\\\")}}"], "scope": "{{scope}}", "description": "d", "source": "s" }""";

    public static string Cache(string id, string path, string category = "cache", string? scope = null) =>
        $$"""{ "id": "{{id}}", "kind": "cache", "paths": ["{{path.Replace("\\", "\\\\")}}"], "category": "{{category}}", {{(scope is null ? "" : $"\"scope\": \"{scope}\",")}} "description": "d", "source": "s" }""";

    public static string Owner(string id, string path, string program = "Sample App", string? scope = null) =>
        $$"""{ "id": "{{id}}", "kind": "ownership", "paths": ["{{path.Replace("\\", "\\\\")}}"], {{(scope is null ? "" : $"\"scope\": \"{scope}\",")}} "program": { "displayName": "{{program}}" }, "description": "d", "source": "s" }""";

    /// <summary>Scans a fictional profile that contains exactly the given folders (relative paths below the profile root).</summary>
    public static FolderNode Tree(IReadOnlyList<string>? personalFolders = null, params string[] relativeFolders)
    {
        var children = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase) { [Root] = new(StringComparer.OrdinalIgnoreCase) };
        foreach (string relative in relativeFolders)
        {
            string parent = Root;
            foreach (string part in relative.Split('\\'))
            {
                children[parent].Add(part);
                string path = parent + "\\" + part;
                if (!children.ContainsKey(path))
                {
                    children[path] = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                parent = path;
            }
        }

        var reader = new FakeDirectoryReader();
        foreach ((string path, SortedSet<string> names) in children)
        {
            reader.Folder(path, [.. names.Select(DirEntry)]);
        }

        return Scan(reader, personalFolders);
    }

    public static FolderNode Scan(FakeDirectoryReader reader, IReadOnlyList<string>? personalFolders = null)
    {
        var options = new ScanOptions { PersonalFolderPaths = personalFolders ?? [] };
        ScanResult result = new ProfileScanner(reader)
            .ScanAsync(new ScanTarget(Root, true), options, null, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        return result.Root!;
    }

    public static FolderNode Find(FolderNode root, string relativePath)
    {
        FolderNode node = root;
        foreach (string part in relativePath.Split('\\'))
        {
            node = node.Children.Single(child => string.Equals(child.Name, part, StringComparison.OrdinalIgnoreCase));
        }

        return node;
    }
}
