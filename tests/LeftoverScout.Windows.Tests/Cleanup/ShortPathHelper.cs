using System.Runtime.InteropServices;
using System.Text;

namespace LeftoverScout.Windows.Tests.Cleanup;

/// <summary>Asks Windows for the 8.3 short form of an existing path, to test short-name handling.</summary>
internal static class ShortPathHelper
{
    // A plain DllImport with a StringBuilder keeps this test helper small; source generation adds nothing for a test.
#pragma warning disable SYSLIB1054
    [DllImport("kernel32.dll", EntryPoint = "GetShortPathNameW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetShortPathName(string longPath, StringBuilder shortPath, uint bufferLength);
#pragma warning restore SYSLIB1054

    /// <summary>Returns the short name of the last segment of <paramref name="path"/>, or null when it has none (8.3 names are off).</summary>
    public static string? GetShortLeafName(string path)
    {
        var buffer = new StringBuilder(1024);
        uint length = GetShortPathName(path, buffer, (uint)buffer.Capacity);
        if (length == 0 || length >= buffer.Capacity)
        {
            return null;
        }

        string shortLeaf = Path.GetFileName(buffer.ToString());
        return string.Equals(shortLeaf, Path.GetFileName(path), StringComparison.OrdinalIgnoreCase) ? null : shortLeaf;
    }
}
