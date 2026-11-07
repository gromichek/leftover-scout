namespace LeftoverScout.Core.Settings;

/// <summary>
/// User settings stored in <c>settings.json</c>.
/// </summary>
/// <param name="SchemaVersion">Version of the file format, for future migrations.</param>
/// <param name="MainWindow">Last position of the main window, or null if it was never saved.</param>
public sealed record AppSettings(int SchemaVersion = AppSettings.CurrentSchemaVersion, WindowPlacement? MainWindow = null)
{
    /// <summary>The schema version written by this version of the application.</summary>
    public const int CurrentSchemaVersion = 1;
}
