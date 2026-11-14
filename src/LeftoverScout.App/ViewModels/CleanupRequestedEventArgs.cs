using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.App.ViewModels;

/// <summary>The selection the user wants to clean up, handed from the Findings page to the Cleanup page.</summary>
public sealed class CleanupRequestedEventArgs : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    /// <param name="request">The selected items.</param>
    /// <param name="sourceResult">The scan result the selection came from; it becomes out of date after the cleanup.</param>
    public CleanupRequestedEventArgs(CleanupRequest request, ScanResult sourceResult)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(sourceResult);
        Request = request;
        SourceResult = sourceResult;
    }

    /// <summary>The selected items.</summary>
    public CleanupRequest Request { get; }

    /// <summary>The scan result the items came from.</summary>
    public ScanResult SourceResult { get; }
}
