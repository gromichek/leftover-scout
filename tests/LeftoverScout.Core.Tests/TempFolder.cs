namespace LeftoverScout.Core.Tests;

/// <summary>
/// A unique temporary folder for one test, removed afterwards. Tests never touch a real user profile.
/// </summary>
public sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LeftoverScoutTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        // The folder was created by this test inside the system temp folder.
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
