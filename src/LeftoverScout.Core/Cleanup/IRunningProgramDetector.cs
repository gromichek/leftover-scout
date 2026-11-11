namespace LeftoverScout.Core.Cleanup;

/// <summary>Finds running programs that seem to belong to the selected items (D-064).</summary>
public interface IRunningProgramDetector
{
    /// <summary>Returns one warning for every item that has at least one matching running process. Names only.</summary>
    IReadOnlyList<RunningProgramWarning> Find(CleanupRequest request);
}
