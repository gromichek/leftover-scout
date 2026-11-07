using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LeftoverScout.Core.Settings;

/// <summary>
/// Stores <see cref="AppSettings"/> as <c>settings.json</c> in the data folder.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    /// <summary>File name of the settings file.</summary>
    public const string FileName = "settings.json";

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly ILogger _logger;

    /// <summary>Creates a store that keeps <c>settings.json</c> in <paramref name="dataFolder"/>.</summary>
    public JsonSettingsStore(string dataFolder, ILogger<JsonSettingsStore> logger)
    {
        _filePath = Path.Combine(dataFolder, FileName);
        _logger = logger;
    }

    /// <inheritdoc />
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogInformation("Settings file not found, using defaults.");
                return new AppSettings();
            }

            string json = File.ReadAllText(_filePath);
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);
            if (settings is null)
            {
                _logger.LogWarning("Settings file is empty, using defaults.");
                return new AppSettings();
            }

            return settings;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Settings file could not be read, using defaults.");
            return new AppSettings();
        }
    }

    /// <inheritdoc />
    public void Save(AppSettings settings)
    {
        string? tempPath = null;
        try
        {
            string? folder = Path.GetDirectoryName(_filePath);
            if (folder is not null)
            {
                Directory.CreateDirectory(folder);
            }

            tempPath = _filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, SerializerOptions));

            // File.Move with overwrite replaces the target in one step on the same volume.
            File.Move(tempPath, _filePath, overwrite: true);
            tempPath = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Settings could not be saved.");
        }
        finally
        {
            RemoveTempFile(tempPath);
        }
    }

    private void RemoveTempFile(string? tempPath)
    {
        if (tempPath is null)
        {
            return;
        }

        // This is the store's own temporary file, never user data.
        try
        {
            File.Delete(tempPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Temporary settings file could not be removed.");
        }
    }
}
