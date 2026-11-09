namespace LeftoverScout.Core.Rules;

/// <summary>The name and JSON text of one rule file.</summary>
/// <param name="Name">File name, for example <c>caches.json</c>.</param>
/// <param name="Json">The JSON text.</param>
public sealed record RuleFile(string Name, string Json);
