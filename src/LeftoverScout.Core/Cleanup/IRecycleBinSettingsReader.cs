namespace LeftoverScout.Core.Cleanup;

/// <summary>Reads the Recycle Bin settings of a drive, read-only.</summary>
public interface IRecycleBinSettingsReader
{
    /// <summary>
    /// Returns the settings of the volume that holds <paramref name="path"/>. Never throws; returns
    /// <see cref="RecycleBinSettings.Unknown"/> when they cannot be read.
    /// </summary>
    RecycleBinSettings Read(string path);
}
