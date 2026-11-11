using LeftoverScout.Core.Cleanup;
using LeftoverScout.Windows.Cleanup;

namespace LeftoverScout.Windows.Tests.Cleanup;

/// <summary>Read-only smoke tests: the reader never throws and never writes.</summary>
public class WindowsRecycleBinSettingsReaderTests
{
    [Fact]
    public void Read_TempFolder_DoesNotThrow()
    {
        RecycleBinSettings settings = new WindowsRecycleBinSettingsReader().Read(Path.GetTempPath());

        Assert.True(settings.MaxCapacityBytes is null or > 0);
    }

    [Fact]
    public void Read_NetworkPath_IsUnknown()
    {
        Assert.Equal(RecycleBinSettings.Unknown, new WindowsRecycleBinSettingsReader().Read(@"\\server\share\folder"));
    }
}
