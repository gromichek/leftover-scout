using System.Runtime.InteropServices;
using System.Windows;
using LeftoverScout.Core.Shell;
using Microsoft.Extensions.Logging;

namespace LeftoverScout.App.Services;

/// <summary>
/// Puts text on the Windows clipboard through WPF.
/// </summary>
/// <remarks>
/// The clipboard can be locked by another program for a moment; the WPF clipboard then throws a
/// <see cref="COMException"/> (an <see cref="ExternalException"/>). <c>Clipboard.SetDataObject</c> with
/// <c>retryTimes</c> and <c>retryDelay</c> retries briefly before it gives up
/// (https://learn.microsoft.com/dotnet/api/system.windows.clipboard.setdataobject).
/// </remarks>
public sealed class WpfClipboardService : IClipboardService
{
    private const int RetryTimes = 5;
    private const int RetryDelayMilliseconds = 50;

    private readonly ILogger<WpfClipboardService> _logger;

    /// <summary>Creates the service.</summary>
    public WpfClipboardService(ILogger<WpfClipboardService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool TrySetText(string text)
    {
        try
        {
            Clipboard.SetDataObject(text, copy: true, RetryTimes, RetryDelayMilliseconds);
            return true;
        }
        catch (ExternalException ex)
        {
            _logger.LogWarning(ex, "The clipboard is busy; the text was not copied.");
            return false;
        }
    }
}
