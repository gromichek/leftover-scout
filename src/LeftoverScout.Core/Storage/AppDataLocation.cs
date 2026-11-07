namespace LeftoverScout.Core.Storage;

/// <summary>
/// The folder where LeftoverScout keeps its settings, logs and journals.
/// </summary>
/// <param name="Path">Absolute path of the data folder.</param>
/// <param name="IsFallback">
/// True when the preferred folder next to the program was not usable and the per-user fallback is used.
/// </param>
public sealed record AppDataLocation(string Path, bool IsFallback);
