namespace LeftoverScout.Core.Rules;

// JSON shapes of the rule files (decision D-040). Enumerations are read as strings so that an unknown
// value becomes a validation error of one rule instead of a failure of the whole file.
internal sealed class RuleFileDto
{
    public int? SchemaVersion { get; set; }

    public List<System.Text.Json.JsonElement>? Rules { get; set; }
}

internal sealed class RuleDto
{
    public string? Id { get; set; }

    public string? Kind { get; set; }

    public List<string?>? Paths { get; set; }

    public string? Scope { get; set; }

    public string? Category { get; set; }

    public ProgramDto? Program { get; set; }

    public string? Description { get; set; }

    public string? Source { get; set; }
}

internal sealed class ProgramDto
{
    public string? DisplayName { get; set; }

    public List<string?>? Names { get; set; }

    public List<string?>? Publishers { get; set; }

    public List<string?>? UninstallKeys { get; set; }

    public List<string?>? PackageFamilies { get; set; }

    public List<string?>? ProgramFolders { get; set; }
}
