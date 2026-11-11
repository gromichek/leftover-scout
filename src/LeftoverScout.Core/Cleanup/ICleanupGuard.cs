namespace LeftoverScout.Core.Cleanup;

/// <summary>The safety gate every deletion passes. Implemented by <see cref="CleanupGuard"/>.</summary>
public interface ICleanupGuard
{
    /// <summary>Checks a request fresh against the rules and the real file system. Changes nothing.</summary>
    CleanupGuardReport Check(CleanupRequest request);
}
