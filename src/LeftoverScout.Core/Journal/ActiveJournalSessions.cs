using System.Collections.Concurrent;

namespace LeftoverScout.Core.Journal;

/// <summary>
/// The sessions that are running in this process. A journal that is <see cref="JournalState.InProgress"/> but not
/// listed here belongs to a session that was cut off (crash, power loss) and is shown as interrupted.
/// </summary>
public sealed class ActiveJournalSessions
{
    private readonly ConcurrentDictionary<Guid, byte> _ids = new();

    /// <summary>Marks a session as running.</summary>
    public void Add(Guid id) => _ids[id] = 0;

    /// <summary>Marks a session as finished.</summary>
    public void Remove(Guid id) => _ids.TryRemove(id, out _);

    /// <summary>True when the session is running in this process.</summary>
    public bool IsActive(Guid id) => _ids.ContainsKey(id);
}
