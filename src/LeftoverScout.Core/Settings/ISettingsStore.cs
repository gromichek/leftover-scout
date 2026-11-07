namespace LeftoverScout.Core.Settings;

/// <summary>
/// Loads and saves <see cref="AppSettings"/>.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Loads the settings. A missing or unreadable file yields default settings; this method never throws.
    /// </summary>
    AppSettings Load();

    /// <summary>
    /// Saves the settings atomically. I/O errors are logged and swallowed; this method never throws.
    /// </summary>
    void Save(AppSettings settings);
}
