namespace LeftoverScout.Core.Cleanup;

/// <summary>Pure decision logic of the Recycle Bin pre-check (D-062 layer 1).</summary>
public static class RecycleBinPolicy
{
    /// <summary>Decides whether an item of <paramref name="sizeBytes"/> may be sent to the Recycle Bin.</summary>
    public static RecycleBinCheck Evaluate(RecycleBinSettings settings, long sizeBytes)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.IsTurnedOff)
        {
            return RecycleBinCheck.TurnedOff;
        }

        return settings.MaxCapacityBytes is { } max && sizeBytes > max
            ? RecycleBinCheck.TooLarge
            : RecycleBinCheck.Allowed;
    }
}
