using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LeftoverScout.Core.Rules;

/// <summary>
/// Parses and validates rule files. An invalid rule is skipped and reported; it never throws (decision D-040).
/// </summary>
/// <remarks>
/// Parsing uses <c>System.Text.Json</c> with <see cref="JsonUnmappedMemberHandling.Disallow"/>, so a misspelled
/// property is reported instead of being ignored
/// (https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/missing-members).
/// </remarks>
public static partial class RuleLoader
{
    /// <summary>The only supported value of <c>schemaVersion</c>.</summary>
    public const int SupportedSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex KebabCase();

    /// <summary>Loads the rules of all files; files are processed in the given order.</summary>
    public static RuleLoadResult Load(IEnumerable<RuleFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        var errors = new List<RuleValidationError>();
        var rules = new List<Rule>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (RuleFile file in files)
        {
            LoadFile(file, rules, seenIds, errors);
        }

        RemoveRulesInsideProtectedSubtrees(rules, errors);
        return new RuleLoadResult(new RuleSet(rules), errors);
    }

    private static void LoadFile(RuleFile file, List<Rule> rules, HashSet<string> seenIds, List<RuleValidationError> errors)
    {
        RuleFileDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<RuleFileDto>(file.Json, JsonOptions);
        }
        catch (JsonException exception)
        {
            errors.Add(new RuleValidationError(file.Name, null, $"The file is not valid: {exception.Message}"));
            return;
        }

        if (dto is null)
        {
            errors.Add(new RuleValidationError(file.Name, null, "The file is empty."));
            return;
        }

        if (dto.SchemaVersion != SupportedSchemaVersion)
        {
            errors.Add(new RuleValidationError(file.Name, null, $"schemaVersion must be {SupportedSchemaVersion}."));
            return;
        }

        if (dto.Rules is null)
        {
            errors.Add(new RuleValidationError(file.Name, null, "The file has no 'rules' list."));
            return;
        }

        for (int index = 0; index < dto.Rules.Count; index++)
        {
            RuleDto? ruleDto;
            try
            {
                ruleDto = dto.Rules[index].Deserialize<RuleDto>(JsonOptions);
            }
            catch (JsonException exception)
            {
                errors.Add(new RuleValidationError(file.Name, null, $"Rule number {index + 1} is not valid: {exception.Message}"));
                continue;
            }

            if (ruleDto is null)
            {
                errors.Add(new RuleValidationError(file.Name, null, $"Rule number {index + 1} is empty."));
                continue;
            }

            var ruleErrors = new List<string>();
            Rule? rule = Validate(ruleDto, ruleErrors);

            if (rule is not null && !seenIds.Add(rule.Id))
            {
                ruleErrors.Add($"Duplicate rule id '{rule.Id}'.");
                rule = null;
            }

            foreach (string message in ruleErrors)
            {
                errors.Add(new RuleValidationError(file.Name, ruleDto.Id, message));
            }

            if (rule is not null)
            {
                rules.Add(rule);
            }
        }
    }

    private static Rule? Validate(RuleDto dto, List<string> errors)
    {
        int before = errors.Count;

        string id = dto.Id ?? string.Empty;
        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            errors.Add("The rule has no id.");
        }
        else if (!KebabCase().IsMatch(id))
        {
            errors.Add($"The id '{id}' must be kebab-case (lower-case letters, digits and single hyphens).");
        }

        RuleKind kind = default;
        bool kindKnown = true;
        switch (dto.Kind)
        {
            case "ownership":
                kind = RuleKind.Ownership;
                break;
            case "cache":
                kind = RuleKind.Cache;
                break;
            case "protected":
                kind = RuleKind.Protected;
                break;
            default:
                kindKnown = false;
                errors.Add(dto.Kind is null ? "The rule has no kind." : $"Unknown kind '{dto.Kind}'.");
                break;
        }

        var patterns = new List<PathPattern>();
        if (dto.Paths is null || dto.Paths.Count == 0)
        {
            errors.Add("The rule needs at least one path.");
        }
        else
        {
            foreach (string? path in dto.Paths)
            {
                if (PathPattern.TryParse(path, out PathPattern? pattern, out string? error))
                {
                    patterns.Add(pattern);
                }
                else
                {
                    errors.Add(error);
                }
            }
        }

        RuleScope? scope = null;
        if (dto.Scope is not null)
        {
            switch (dto.Scope)
            {
                case "folder":
                    scope = RuleScope.Folder;
                    break;
                case "subtree":
                    scope = RuleScope.Subtree;
                    break;
                default:
                    errors.Add($"Unknown scope '{dto.Scope}'.");
                    break;
            }
        }

        if (kindKnown && kind == RuleKind.Protected && dto.Scope is null)
        {
            errors.Add("A protected rule needs a scope ('folder' or 'subtree').");
        }

        CacheCategory? category = null;
        if (dto.Category is not null)
        {
            switch (dto.Category)
            {
                case "cache":
                    category = CacheCategory.Cache;
                    break;
                case "temp":
                    category = CacheCategory.Temp;
                    break;
                case "logs":
                    category = CacheCategory.Logs;
                    break;
                case "crashDumps":
                    category = CacheCategory.CrashDumps;
                    break;
                default:
                    errors.Add($"Unknown category '{dto.Category}'.");
                    break;
            }
        }

        if (kindKnown && kind == RuleKind.Cache && dto.Category is null)
        {
            errors.Add("A cache rule needs a category (cache, temp, logs or crashDumps).");
        }

        if (kindKnown && kind != RuleKind.Cache && dto.Category is not null)
        {
            errors.Add("Only cache rules have a category.");
        }

        ProgramHints? program = null;
        if (dto.Program is not null)
        {
            program = ValidateProgram(dto.Program, kindKnown && kind == RuleKind.Ownership, errors);
        }

        if (kindKnown && kind == RuleKind.Ownership && dto.Program is null)
        {
            errors.Add("An ownership rule needs a program.");
        }

        if (kindKnown && kind == RuleKind.Protected && dto.Program is not null)
        {
            errors.Add("A protected rule must not have a program.");
        }

        if (string.IsNullOrWhiteSpace(dto.Description))
        {
            errors.Add("The rule needs a description.");
        }

        if (string.IsNullOrWhiteSpace(dto.Source))
        {
            errors.Add("The rule needs a source.");
        }

        if (errors.Count > before)
        {
            return null;
        }

        return new Rule(
            id,
            kind,
            patterns,
            scope ?? RuleScope.Subtree,
            category,
            program,
            dto.Description!.Trim(),
            dto.Source!.Trim());
    }

    private static ProgramHints? ValidateProgram(ProgramDto dto, bool requireInstallationFlag, List<string> errors)
    {
        int before = errors.Count;

        if (string.IsNullOrWhiteSpace(dto.DisplayName))
        {
            errors.Add("The program needs a displayName.");
        }

        if (requireInstallationFlag && dto.RequiresInstallation is null)
        {
            errors.Add("The program of an ownership rule needs 'requiresInstallation' (true or false).");
        }

        IReadOnlyList<string> names = CleanHints(dto.Names, "names", errors);
        IReadOnlyList<string> publishers = CleanHints(dto.Publishers, "publishers", errors);
        IReadOnlyList<string> uninstallKeys = CleanHints(dto.UninstallKeys, "uninstallKeys", errors);
        IReadOnlyList<string> packageFamilies = CleanHints(dto.PackageFamilies, "packageFamilies", errors);
        IReadOnlyList<string> programFolders = CleanHints(dto.ProgramFolders, "programFolders", errors);

        return errors.Count > before
            ? null
            : new ProgramHints(dto.DisplayName!.Trim(), dto.RequiresInstallation ?? false, names, publishers, uninstallKeys, packageFamilies, programFolders);
    }

    private static List<string> CleanHints(List<string?>? values, string listName, List<string> errors)
    {
        var result = new List<string>();
        if (values is null)
        {
            return result;
        }

        foreach (string? value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"The program list '{listName}' has an empty entry.");
            }
            else if (value.Contains('*') || value.Contains('?'))
            {
                errors.Add($"The program list '{listName}' has the entry '{value}' with a wildcard; hints are plain text.");
            }
            else
            {
                result.Add(value.Trim());
            }
        }

        return result;
    }

    // Protection is absolute: a cache or ownership rule may not reach into a protected subtree.
    private static void RemoveRulesInsideProtectedSubtrees(List<Rule> rules, List<RuleValidationError> errors)
    {
        List<PathPattern> protectedSubtrees = rules
            .Where(rule => rule.Kind == RuleKind.Protected && rule.Scope == RuleScope.Subtree)
            .SelectMany(rule => rule.Paths)
            .ToList();

        if (protectedSubtrees.Count == 0)
        {
            return;
        }

        for (int i = rules.Count - 1; i >= 0; i--)
        {
            Rule rule = rules[i];
            if (rule.Kind == RuleKind.Protected)
            {
                continue;
            }

            foreach (PathPattern path in rule.Paths)
            {
                PathPattern? conflict = protectedSubtrees.FirstOrDefault(path.MayLieInside);
                if (conflict is not null)
                {
                    errors.Add(new RuleValidationError(
                        string.Empty,
                        rule.Id,
                        $"The path '{path}' lies inside the protected subtree '{conflict}'. Protection is absolute."));
                    rules.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
