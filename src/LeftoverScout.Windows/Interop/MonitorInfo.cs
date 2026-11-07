namespace LeftoverScout.Windows.Interop;

/// <summary>MONITORINFO.</summary>
internal struct MonitorInfo
{
    public uint Size;
    public Rect Monitor;
    public Rect Work;
    public uint Flags;
}
