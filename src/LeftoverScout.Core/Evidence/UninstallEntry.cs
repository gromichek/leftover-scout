namespace LeftoverScout.Core.Evidence;

/// <summary>
/// The values of one subkey of an Uninstall key that the parser needs. Missing or non-text values are null.
/// </summary>
/// <param name="KeyName">Name of the subkey.</param>
/// <param name="DisplayName">DisplayName value.</param>
/// <param name="Publisher">Publisher value.</param>
/// <param name="DisplayVersion">DisplayVersion value.</param>
/// <param name="InstallLocation">InstallLocation value.</param>
/// <param name="DisplayIcon">DisplayIcon value.</param>
/// <param name="InstallDate">InstallDate value as text (usually yyyyMMdd).</param>
/// <param name="ParentKeyName">ParentKeyName value (set for updates and patches).</param>
/// <param name="ReleaseType">ReleaseType value (for example "Update" or "Hotfix").</param>
/// <param name="SystemComponent">SystemComponent value; null when missing.</param>
public sealed record UninstallEntry(
    string KeyName,
    string? DisplayName,
    string? Publisher = null,
    string? DisplayVersion = null,
    string? InstallLocation = null,
    string? DisplayIcon = null,
    string? InstallDate = null,
    string? ParentKeyName = null,
    string? ReleaseType = null,
    int? SystemComponent = null);
