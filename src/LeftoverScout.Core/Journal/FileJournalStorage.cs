using System.Text;

namespace LeftoverScout.Core.Journal;

/// <summary>Keeps journal files in a folder on disk (<c>data\journal</c>).</summary>
public sealed class FileJournalStorage : IJournalStorage
{
    /// <summary>Name of the journal folder inside the data folder.</summary>
    public const string FolderName = "journal";

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Creates storage for the journal folder inside <paramref name="dataFolder"/>.</summary>
    public FileJournalStorage(string dataFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataFolder);
        FolderPath = Path.Combine(dataFolder, FolderName);
    }

    /// <inheritdoc />
    public string FolderPath { get; }

    /// <inheritdoc />
    public bool EnsureFolder()
    {
        try
        {
            Directory.CreateDirectory(FolderPath);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void WriteAtomic(string fileName, string content)
    {
        Directory.CreateDirectory(FolderPath);
        string target = Path.Combine(FolderPath, fileName);
        string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, content, Utf8NoBom);

            // Replaces the target in one step on the same volume.
            File.Move(temp, target, overwrite: true);
            temp = string.Empty;
        }
        finally
        {
            if (temp.Length > 0)
            {
                // The journal's own temporary file, never user data.
                try
                {
                    File.Delete(temp);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // Nothing more can be done; the leftover temporary file is ignored by the reader.
                }
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ListFileNames()
    {
        try
        {
            if (!Directory.Exists(FolderPath))
            {
                return [];
            }

            return Directory
                .EnumerateFiles(FolderPath, JournalFileName.SearchPattern)
                .Select(path => Path.GetFileName(path) ?? string.Empty)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <inheritdoc />
    public string ReadAllText(string fileName) => File.ReadAllText(Path.Combine(FolderPath, fileName), Encoding.UTF8);
}
