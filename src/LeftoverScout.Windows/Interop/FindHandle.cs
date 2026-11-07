using Microsoft.Win32.SafeHandles;

namespace LeftoverScout.Windows.Interop;

/// <summary>
/// A handle returned by FindFirstFileExW. Closed with FindClose.
/// </summary>
internal sealed class FindHandle : SafeHandleMinusOneIsInvalid
{
    /// <summary>Creates an empty handle (used by the P/Invoke marshaller).</summary>
    public FindHandle()
        : base(ownsHandle: true)
    {
    }

    /// <inheritdoc />
    protected override bool ReleaseHandle() => NativeMethods.FindClose(handle);
}
