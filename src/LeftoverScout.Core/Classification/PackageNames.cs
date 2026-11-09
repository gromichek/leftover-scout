namespace LeftoverScout.Core.Classification;

/// <summary>Splits the name of a Store package folder (<c>Name_publisherId</c>) into its parts (decision D-050).</summary>
public static class PackageNames
{
    /// <summary>The package name: the folder name before the last <c>_</c> (the whole name when there is none).</summary>
    public static string PackageName(string folderName)
    {
        ArgumentNullException.ThrowIfNull(folderName);
        int underscore = folderName.LastIndexOf('_');
        return underscore > 0 ? folderName[..underscore] : folderName;
    }

    /// <summary>The vendor part: the package name before its first <c>.</c> (the whole name when there is no dot).</summary>
    public static string VendorPart(string folderName)
    {
        string name = PackageName(folderName);
        int dot = name.IndexOf('.', StringComparison.Ordinal);
        return dot > 0 ? name[..dot] : name;
    }
}
