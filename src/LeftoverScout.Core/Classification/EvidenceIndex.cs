using LeftoverScout.Core.Evidence;

namespace LeftoverScout.Core.Classification;

/// <summary>
/// The names found in the evidence, prepared once per scan for fast comparison (decision D-045). A folder name matches
/// when the joined forms are equal, when all significant tokens of the folder name are tokens of one entry, or when all
/// significant tokens of an entry are tokens of the folder name. There is no fuzzy similarity.
/// </summary>
public sealed class EvidenceIndex
{
    private const int MinimumTokenLength = 3;

    private readonly List<Entry> _entries = [];
    private readonly Dictionary<string, string> _storeFamilies = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Builds the index from <paramref name="snapshot"/>.</summary>
    public EvidenceIndex(EvidenceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        foreach (InstalledProgram program in snapshot.Programs)
        {
            bool vendorEvidence = program.Source is ProgramSource.Registry or ProgramSource.Store;
            AddName(program.Name, program, isPublisher: false, vendorEvidence);
            AddName(program.Publisher, program, isPublisher: true, vendorEvidence);

            switch (program.Source)
            {
                case ProgramSource.Registry:
                    AddName(RegistryKeyName(program.SourceDetail), program, isPublisher: false, vendorEvidence: true);
                    break;

                case ProgramSource.Store:
                    _storeFamilies.TryAdd(program.SourceDetail, program.Name);
                    AddName(program.SourceDetail.Split('_')[0], program, isPublisher: false);
                    break;
            }

            foreach (string subfolder in program.SubfolderNames)
            {
                AddName(subfolder, program, isPublisher: false);
            }
        }
    }

    /// <summary>Number of indexed names.</summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Finds the installed program a folder name belongs to; null when nothing matches. The caller decides what to do
    /// with generic names (<see cref="GenericNames"/>); this method only compares.
    /// </summary>
    public EvidenceMatch? Match(NormalizedName folderName)
    {
        ArgumentNullException.ThrowIfNull(folderName);
        return Find(folderName, includePublishers: true);
    }

    /// <summary>
    /// True when a hint of an ownership rule (a program name, uninstall key, package family or folder name) matches
    /// evidence. Publisher names are not compared: a publisher is not the identity of one program.
    /// </summary>
    public bool MatchesHint(string hint)
    {
        ArgumentNullException.ThrowIfNull(hint);
        return FindStoreFamily(hint) is not null || Find(NameNormalizer.Normalize(hint), includePublishers: false) is not null;
    }

    private EvidenceMatch? Find(NormalizedName folderName, bool includePublishers)
    {
        if (folderName.IsEmpty)
        {
            return null;
        }

        string[] folderSignificant = Significant(folderName.Tokens);
        Entry? best = null;
        bool anyName = false;

        foreach (Entry entry in _entries)
        {
            if ((entry.IsPublisher && !includePublishers) || !Matches(folderName, folderSignificant, entry))
            {
                continue;
            }

            anyName |= !entry.IsPublisher;
            if (best is null || Rank(entry) < Rank(best))
            {
                best = entry;
            }
        }

        return best is null ? null : new EvidenceMatch(best.Program.Name, OnlyPublisher: !anyName);
    }

    /// <summary>
    /// Compares the vendor part of a Store package name (decision D-050) with the <b>name, publisher or registry key name</b>
    /// of installed programs (registry and Store sources only; never Start menu or folder names). The vendor must appear as a
    /// whole token; null when nothing matches or when the vendor is generic or shorter than 3 characters.
    /// </summary>
    public EvidenceMatch? MatchVendor(string vendorPart)
    {
        ArgumentNullException.ThrowIfNull(vendorPart);
        NormalizedName vendor = NameNormalizer.Normalize(vendorPart);
        string[] significant = Significant(vendor.Tokens);
        if (vendor.IsEmpty || vendor.Joined.Length < MinimumTokenLength || GenericNames.IsGeneric(vendor) || significant.Length == 0)
        {
            return null;
        }

        Entry? best = null;
        bool anyName = false;
        foreach (Entry entry in _entries)
        {
            if (!entry.VendorEvidence
                || !(vendor.Joined == entry.Name.Joined || significant.All(token => entry.Name.Tokens.Contains(token))))
            {
                continue;
            }

            anyName |= !entry.IsPublisher;
            if (best is null || Rank(entry) < Rank(best))
            {
                best = entry;
            }
        }

        return best is null ? null : new EvidenceMatch(best.Program.Name, OnlyPublisher: !anyName);
    }

    /// <summary>Finds the installed Store package with this family name (case-insensitive); null when there is none.</summary>
    public string? FindStoreFamily(string familyName) =>
        _storeFamilies.GetValueOrDefault(familyName);

    private static bool Matches(NormalizedName folder, string[] folderSignificant, Entry entry)
    {
        if (folder.Joined == entry.Name.Joined)
        {
            return true;
        }

        if (folderSignificant.Length > 0 && folderSignificant.All(token => entry.Name.Tokens.Contains(token)))
        {
            return true;
        }

        return entry.Significant.Length > 0 && entry.Significant.All(token => folder.Tokens.Contains(token));
    }

    // Lower is better: names before publishers, then registry, Store, program folders, Start menu.
    private static int Rank(Entry entry) => (entry.IsPublisher ? 10 : 0) + (int)entry.Program.Source;

    private void AddName(string? text, InstalledProgram program, bool isPublisher, bool vendorEvidence = false)
    {
        NormalizedName name = NameNormalizer.Normalize(text);
        if (!name.IsEmpty)
        {
            _entries.Add(new Entry(name, Significant(name.Tokens), program, isPublisher, vendorEvidence));
        }
    }

    // Tokens that may take part in "contains" checks: at least three characters and not a generic word.
    private static string[] Significant(IReadOnlyList<string> tokens) =>
        [.. tokens.Where(token => token.Length >= MinimumTokenLength && !GenericNames.IsGenericToken(token))];

    // "HKLM\SOFTWARE\...\Uninstall\Name (64-bit view)" -> "Name".
    private static string? RegistryKeyName(string sourceDetail)
    {
        string text = sourceDetail;
        int view = text.LastIndexOf(" (", StringComparison.Ordinal);
        if (view > 0 && text.EndsWith(" view)", StringComparison.Ordinal))
        {
            text = text[..view];
        }

        int separator = text.LastIndexOf('\\');
        return separator >= 0 ? text[(separator + 1)..] : text;
    }

    private sealed record Entry(NormalizedName Name, string[] Significant, InstalledProgram Program, bool IsPublisher, bool VendorEvidence);
}
