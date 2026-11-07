using System.Runtime.InteropServices;
using LeftoverScout.Core.UI;
using LeftoverScout.Windows.Interop;

namespace LeftoverScout.Windows.UI;

/// <summary>
/// Lists the work area (screen minus taskbar) of every monitor, so a window position can be checked against
/// the monitors that really exist and not against one rectangle around all of them.
/// Uses EnumDisplayMonitors and GetMonitorInfo (Microsoft Learn).
/// </summary>
public static class MonitorWorkAreas
{
    /// <summary>Returns the work area of each monitor in physical pixels (virtual screen coordinates).</summary>
    public static IReadOnlyList<ScreenArea> GetPixelAreas()
    {
        var areas = new List<ScreenArea>();
        GCHandle handle = GCHandle.Alloc(areas);
        try
        {
            unsafe
            {
                NativeMethods.EnumDisplayMonitors(0, 0, &EnumerateMonitor, GCHandle.ToIntPtr(handle));
            }
        }
        finally
        {
            handle.Free();
        }

        return areas;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    private static unsafe int EnumerateMonitor(nint monitor, nint deviceContext, Rect* bounds, nint data)
    {
        try
        {
            var areas = (List<ScreenArea>)GCHandle.FromIntPtr(data).Target!;
            var info = new MonitorInfo { Size = (uint)sizeof(MonitorInfo) };
            if (NativeMethods.GetMonitorInfo(monitor, ref info))
            {
                areas.Add(new ScreenArea(
                    info.Work.Left,
                    info.Work.Top,
                    info.Work.Right - info.Work.Left,
                    info.Work.Bottom - info.Work.Top));
            }

            return 1;
        }
        catch (Exception)
        {
            // An exception must not cross the native boundary; stop enumerating instead.
            return 0;
        }
    }
}
