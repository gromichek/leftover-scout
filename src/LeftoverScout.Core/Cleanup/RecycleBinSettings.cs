namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// The Recycle Bin settings of the drive an item lives on, for the current user (D-062 layer 1).
/// </summary>
/// <param name="IsTurnedOff">True when files are deleted without the bin ("Don't move files to the Recycle Bin", or the matching policy).</param>
/// <param name="MaxCapacityBytes">The maximum size of the bin in bytes; null when unknown.</param>
public sealed record RecycleBinSettings(bool IsTurnedOff, long? MaxCapacityBytes)
{
    /// <summary>Nothing known: the pre-check allows the item and layers 2 and 3 protect.</summary>
    public static RecycleBinSettings Unknown { get; } = new(false, null);
}
