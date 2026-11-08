using System.Globalization;
using LeftoverScout.Core.Evidence;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// A row of the Programs table. Texts are never null so that the grid sorts them without special cases;
/// the install date also exposes a raw value for sorting.
/// </summary>
public sealed class ProgramRowViewModel
{
    /// <summary>Creates the row view model.</summary>
    public ProgramRowViewModel(InstalledProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        Program = program;
        Installed = program.InstallDate is { } date ? date.ToDateTime(TimeOnly.MinValue) : null;
        InstalledText = Installed is { } installed ? installed.ToString("d", CultureInfo.CurrentCulture) : string.Empty;
    }

    /// <summary>The underlying program.</summary>
    public InstalledProgram Program { get; }

    /// <summary>Program name.</summary>
    public string Name => Program.Name;

    /// <summary>Publisher; empty when unknown.</summary>
    public string Publisher => Program.Publisher ?? string.Empty;

    /// <summary>Version; empty when unknown.</summary>
    public string Version => Program.Version ?? string.Empty;

    /// <summary>Source text, for example "Program folder".</summary>
    public string SourceText => ProgramFilter.SourceText(Program.Source);

    /// <summary>Install location; empty when unknown.</summary>
    public string InstallLocation => Program.InstallLocation ?? string.Empty;

    /// <summary>Install date (sort value); null when unknown.</summary>
    public DateTime? Installed { get; }

    /// <summary>Install date in the short date format of the current culture; empty when unknown.</summary>
    public string InstalledText { get; }
}
