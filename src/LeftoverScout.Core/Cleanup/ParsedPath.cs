namespace LeftoverScout.Core.Cleanup;

/// <summary>The outcome of parsing a path.</summary>
internal enum PathParseResult
{
    /// <summary>A plain, fully qualified path.</summary>
    Ok,

    /// <summary>Not a plain fully qualified path.</summary>
    Invalid,

    /// <summary>A device or extended-length path.</summary>
    Device,
}

/// <summary>
/// A fully qualified path split into a root (<c>C:</c> or <c>\\server\share</c>) and folder names. Parsing is pure
/// text work and independent of the operating system; <c>/</c> is read as <c>\</c>. Anything that Windows could read
/// differently from what it looks like (<c>..</c>, wildcards, trailing dots or spaces, streams, device names,
/// the <c>\\?\</c> prefix) is rejected instead of being normalized.
/// </summary>
internal sealed class ParsedPath
{
    private static readonly string[] ReservedDeviceNames =
    [
        "CON", "PRN", "AUX", "NUL",
        "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    ];

    private const string InvalidSegmentCharacters = "*?<>|\":";

    private ParsedPath(string root, string[] segments)
    {
        Root = root;
        Segments = segments;
    }

    /// <summary>The drive (<c>C:</c>) or the UNC share (<c>\\server\share</c>).</summary>
    public string Root { get; }

    /// <summary>The folder names below the root.</summary>
    public string[] Segments { get; }

    /// <summary>The path as text, without a trailing separator except for a bare root.</summary>
    public string Text => Segments.Length == 0 ? Root + "\\" : Root + "\\" + string.Join('\\', Segments);

    /// <summary>A key for case-insensitive comparison (use <see cref="StringComparer.OrdinalIgnoreCase"/>).</summary>
    public string Key => Text;

    /// <summary>Parses <paramref name="path"/>.</summary>
    public static PathParseResult TryParse(string? path, out ParsedPath? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            return PathParseResult.Invalid;
        }

        string text = path.Replace('/', '\\');
        if (text.StartsWith(@"\\?\", StringComparison.Ordinal)
            || text.StartsWith(@"\\.\", StringComparison.Ordinal)
            || text.StartsWith(@"\??\", StringComparison.Ordinal))
        {
            return PathParseResult.Device;
        }

        string root;
        string[] parts;
        if (text.StartsWith(@"\\", StringComparison.Ordinal))
        {
            string[] unc = text[2..].Split('\\');
            if (unc.Length < 2)
            {
                return PathParseResult.Invalid;
            }

            root = @"\\" + unc[0] + "\\" + unc[1];
            parts = unc[2..];
            if (!IsValidSegment(unc[0], out PathParseResult serverResult) || !IsValidSegment(unc[1], out serverResult))
            {
                return serverResult;
            }
        }
        else if (text.Length >= 3 && char.IsAsciiLetter(text[0]) && text[1] == ':' && text[2] == '\\')
        {
            root = text[..2];
            parts = text[3..].Split('\\');
        }
        else
        {
            // Relative, rooted without a drive, or drive-relative ("C:folder").
            return PathParseResult.Invalid;
        }

        List<string> segments = [.. parts];
        if (segments.Count > 0 && segments[^1].Length == 0)
        {
            segments.RemoveAt(segments.Count - 1);
        }

        foreach (string segment in segments)
        {
            if (!IsValidSegment(segment, out PathParseResult result))
            {
                return result;
            }
        }

        parsed = new ParsedPath(root, [.. segments]);
        return PathParseResult.Ok;
    }

    /// <summary>Parses a path that is known to be valid, or returns null.</summary>
    public static ParsedPath? ParseOrNull(string? path) =>
        TryParse(path, out ParsedPath? parsed) == PathParseResult.Ok ? parsed : null;

    /// <summary>The path made of the first <paramref name="segmentCount"/> folder names.</summary>
    public ParsedPath Prefix(int segmentCount) => new(Root, Segments[..segmentCount]);

    /// <summary>True when both paths name the same folder (case-insensitive).</summary>
    public bool IsSameAs(ParsedPath other) =>
        Segments.Length == other.Segments.Length && IsAtOrInside(other);

    /// <summary>True when this path is <paramref name="ancestor"/> or lies below it.</summary>
    public bool IsAtOrInside(ParsedPath ancestor) =>
        string.Equals(Root, ancestor.Root, StringComparison.OrdinalIgnoreCase)
        && StartsWith(Segments, ancestor.Segments);

    /// <summary>True when this path lies below <paramref name="ancestor"/> (not equal to it).</summary>
    public bool IsStrictlyInside(ParsedPath ancestor) =>
        Segments.Length > ancestor.Segments.Length && IsAtOrInside(ancestor);

    /// <summary>True when <paramref name="segments"/> begins with <paramref name="prefix"/>, comparing whole names.</summary>
    public static bool StartsWith(IReadOnlyList<string> segments, IReadOnlyList<string> prefix)
    {
        if (segments.Count < prefix.Count)
        {
            return false;
        }

        for (int i = 0; i < prefix.Count; i++)
        {
            if (!string.Equals(segments[i], prefix[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override string ToString() => Text;

    private static bool IsValidSegment(string segment, out PathParseResult result)
    {
        result = PathParseResult.Invalid;
        if (segment.Length == 0 || segment is "." or "..")
        {
            return false;
        }

        if (segment[^1] is '.' or ' ')
        {
            return false;
        }

        foreach (char c in segment)
        {
            if (c < ' ' || InvalidSegmentCharacters.Contains(c))
            {
                return false;
            }
        }

        int dot = segment.IndexOf('.', StringComparison.Ordinal);
        string baseName = (dot < 0 ? segment : segment[..dot]).TrimEnd(' ');
        if (ReservedDeviceNames.Contains(baseName, StringComparer.OrdinalIgnoreCase))
        {
            result = PathParseResult.Device;
            return false;
        }

        result = PathParseResult.Ok;
        return true;
    }
}
