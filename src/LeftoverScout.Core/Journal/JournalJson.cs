using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeftoverScout.Core.Journal;

/// <summary>
/// Reads and writes the journal JSON (UTF-8, indented). Unknown properties, unknown enum names and unknown
/// schema versions are rejected when reading, so a file the app does not understand is never half-interpreted.
/// </summary>
public static class JournalJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Serializes a journal document.</summary>
    public static string Serialize(JournalDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return JsonSerializer.Serialize(document, Options);
    }

    /// <summary>
    /// Parses a journal document. Returns null when the text is not valid JSON, does not match the format, or has
    /// another schema version. Never throws.
    /// </summary>
    public static JournalDocument? TryParse(string json)
    {
        try
        {
            JournalDocument? document = JsonSerializer.Deserialize<JournalDocument>(json, Options);
            return document is { SchemaVersion: JournalDocument.CurrentSchemaVersion, Items: not null, Totals: not null }
                ? document
                : null;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or ArgumentException)
        {
            return null;
        }
    }
}
