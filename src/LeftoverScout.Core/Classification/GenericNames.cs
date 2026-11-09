namespace LeftoverScout.Core.Classification;

/// <summary>
/// Folder names that say nothing about the program they belong to (decision D-045). Such a name never decides anything.
/// </summary>
internal static class GenericNames
{
    private static readonly HashSet<string> Words = new(StringComparer.Ordinal)
    {
        "temp", "tmp", "cache", "caches", "logs", "log", "data", "crashpad", "crashdumps", "updates", "update",
        "packages", "programs", "microsoft", "windows", "common", "shared", "local", "roaming", "default", "user",
        "users", "app", "apps",
    };

    /// <summary>True when <paramref name="token"/> is a generic word.</summary>
    public static bool IsGenericToken(string token) => Words.Contains(token);

    /// <summary>True when the name has tokens and either all of them are generic words or the joined name is one (<c>CrashDumps</c>).</summary>
    public static bool IsGeneric(NormalizedName name) =>
        !name.IsEmpty && (Words.Contains(name.Joined) || name.Tokens.All(token => Words.Contains(token)));
}
