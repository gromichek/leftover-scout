using LeftoverScout.Core.Classification;

namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// A running process matches an item when its program file lies inside the item, or when its normalized name equals
/// the normalized program name of the item (decision D-064). Generic names never match by name. No process is touched.
/// </summary>
public sealed class RunningProgramDetector : IRunningProgramDetector
{
    private readonly IProcessLister _processes;

    /// <summary>Creates the detector.</summary>
    public RunningProgramDetector(IProcessLister processes)
    {
        ArgumentNullException.ThrowIfNull(processes);
        _processes = processes;
    }

    /// <inheritdoc />
    public IReadOnlyList<RunningProgramWarning> Find(CleanupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<ProcessInfo> processes = _processes.GetProcesses();
        var warnings = new List<RunningProgramWarning>();

        foreach (CleanupItem item in request.Items)
        {
            ParsedPath? itemPath = ParsedPath.ParseOrNull(item.FullPath);
            NormalizedName program = NameNormalizer.Normalize(item.ProgramName ?? LastSegment(item.FullPath));
            bool nameUsable = !program.IsEmpty && !GenericNames.IsGeneric(program);

            var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ProcessInfo process in processes)
            {
                if (Matches(process, itemPath, program, nameUsable))
                {
                    names.Add(process.Name);
                }
            }

            if (names.Count > 0)
            {
                warnings.Add(new RunningProgramWarning(item, [.. names]));
            }
        }

        return warnings;
    }

    private static bool Matches(ProcessInfo process, ParsedPath? itemPath, NormalizedName program, bool nameUsable)
    {
        if (itemPath is not null && ParsedPath.ParseOrNull(process.ImagePath) is { } image && image.IsStrictlyInside(itemPath))
        {
            return true;
        }

        if (!nameUsable)
        {
            return false;
        }

        string name = process.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? process.Name[..^4] : process.Name;
        return string.Equals(NameNormalizer.Normalize(name).Joined, program.Joined, StringComparison.Ordinal);
    }

    private static string LastSegment(string path)
    {
        string trimmed = path.TrimEnd('\\', '/');
        int cut = trimmed.LastIndexOfAny(['\\', '/']);
        return cut >= 0 ? trimmed[(cut + 1)..] : trimmed;
    }
}
