using LeftoverScout.Core.Logging;
using Microsoft.Extensions.Logging;

namespace LeftoverScout.Core.Tests.Logging;

public class FileLoggerProviderTests
{
    [Fact]
    public void Log_WritesOneLinePerEntryToDatedFile()
    {
        using var temp = new TempFolder();
        string logs = Path.Combine(temp.Path, "logs");
        using var provider = new FileLoggerProvider(logs);
        ILogger logger = provider.CreateLogger("Sample.Category");

        logger.LogInformation("First entry");
        logger.LogError(new InvalidOperationException("boom"), "Second {Value}", 42);

        string file = Assert.Single(Directory.GetFiles(logs));
        Assert.Matches(@"^leftoverscout-\d{8}\.log$", Path.GetFileName(file));

        string[] lines = File.ReadAllLines(file);
        Assert.Equal(2, lines.Length);
        Assert.Contains("[Information] Sample.Category: First entry", lines[0], StringComparison.Ordinal);
        Assert.Contains("[Error] Sample.Category: Second 42", lines[1], StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void Log_FolderCannotBeCreated_DoesNotThrow()
    {
        using var temp = new TempFolder();
        string blocker = Path.Combine(temp.Path, "blocker");
        File.WriteAllText(blocker, "a file where the folder should be");
        using var provider = new FileLoggerProvider(Path.Combine(blocker, "logs"));

        provider.CreateLogger("Sample").LogWarning("ignored");
    }

    [Fact]
    public void FormatLine_MultilineMessage_StaysOnOneLine()
    {
        string line = FileLoggerProvider.FormatLine(
            new DateTimeOffset(2026, 11, 7, 12, 0, 0, TimeSpan.Zero), LogLevel.Warning, "Sample", "a\r\nb\nc", null);

        Assert.DoesNotContain('\n', line);
        Assert.DoesNotContain('\r', line);
    }
}
