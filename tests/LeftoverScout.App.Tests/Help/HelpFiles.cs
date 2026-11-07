namespace LeftoverScout.App.Tests.Help;

/// <summary>Locates the help files in the repository (the same files are embedded into the app).</summary>
internal static class HelpFiles
{
    public static string UserGuideFolder { get; } = FindUserGuideFolder();

    public static IReadOnlyList<string> Files(string language) =>
        Directory.GetFiles(System.IO.Path.Combine(UserGuideFolder, language), "*.md")
            .Select(System.IO.Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static string ReadText(string language, string fileName) =>
        File.ReadAllText(System.IO.Path.Combine(UserGuideFolder, language, fileName));

    private static string FindUserGuideFolder()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory is not null)
        {
            if (File.Exists(System.IO.Path.Combine(directory, "LeftoverScout.slnx")))
            {
                return System.IO.Path.Combine(directory, "docs", "user-guide");
            }

            directory = System.IO.Path.GetDirectoryName(directory);
        }

        throw new InvalidOperationException("Repository root (LeftoverScout.slnx) not found.");
    }
}
