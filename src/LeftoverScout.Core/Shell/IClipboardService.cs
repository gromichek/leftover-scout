namespace LeftoverScout.Core.Shell;

/// <summary>
/// Puts text on the clipboard.
/// </summary>
public interface IClipboardService
{
    /// <summary>Copies <paramref name="text"/> to the clipboard. Returns false (and logs) on failure; never throws.</summary>
    bool TrySetText(string text);
}
