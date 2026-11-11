using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Rules;

/// <summary>
/// Matches the rules against a scan tree (decision D-041). Names are compared as whole path segments, ordinal and
/// case-insensitive. The tree is walked once; the rules are indexed in a trie by their path segments, so the cost
/// does not grow with the number of rules times the number of folders.
/// </summary>
public sealed class RuleMatcher
{
    private static readonly TrieNode[] NoStates = [];
    private static readonly RuleMatch[] NoMatches = [];

    private readonly TrieNode _trieRoot = new();

    /// <summary>The built-in rule that protects personal folders (decision D-020).</summary>
    public static Rule PersonalFolderRule { get; } = new(
        "personal-folder",
        RuleKind.Protected,
        [],
        RuleScope.Subtree,
        Category: null,
        Program: null,
        "One of your own folders (Documents, Desktop, Pictures, Videos, Music, Downloads or OneDrive). Personal folders are always protected.",
        "personal folder");

    /// <summary>Indexes the rules.</summary>
    public RuleMatcher(RuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);

        foreach (Rule rule in ruleSet.Rules)
        {
            foreach (PathPattern pattern in rule.Paths)
            {
                TrieNode node = _trieRoot;
                foreach (string segment in pattern.Segments)
                {
                    node = segment == "*" ? node.WildcardChild() : node.LiteralChild(segment);
                }

                node.Terminals.Add(new RuleMatch(rule, pattern, IsInherited: false));
            }
        }

        MarkProtectedBelow(_trieRoot);
    }

    /// <summary>
    /// Finds the rules of one folder from its path only, without a scan tree. Used by the cleanup guard as a check that
    /// is independent of the scan result. Subtree rules of every ancestor are returned as inherited. The built-in
    /// protection of personal folders is not part of this match (the guard checks personal folders itself).
    /// </summary>
    /// <param name="relativeSegments">The folder names below the profile root; empty for the root itself.</param>
    /// <returns>The matching rules, or null when no rule applies.</returns>
    public FolderRuleMatch? MatchPath(IReadOnlyList<string> relativeSegments)
    {
        ArgumentNullException.ThrowIfNull(relativeSegments);

        TrieNode[] states = [_trieRoot];
        List<RuleMatch>? inherited = null;
        List<RuleMatch>? direct = null;
        for (int depth = 0; depth <= relativeSegments.Count; depth++)
        {
            bool isFolder = depth == relativeSegments.Count;
            foreach (TrieNode state in states)
            {
                foreach (RuleMatch terminal in state.Terminals)
                {
                    if (isFolder)
                    {
                        (direct ??= []).Add(terminal);
                    }
                    else if (terminal.Rule.Scope == RuleScope.Subtree)
                    {
                        (inherited ??= []).Add(terminal with { IsInherited = true });
                    }
                }
            }

            if (!isFolder)
            {
                states = Advance(states, relativeSegments[depth]);
            }
        }

        return inherited is null && direct is null
            ? null
            : new FolderRuleMatch((inherited ?? []).Concat(direct ?? []));
    }

    /// <summary>
    /// True when a protected rule names a folder strictly below the given folder, so that deleting the folder would
    /// delete a protected folder. A <c>*</c> in a rule counts as matching.
    /// </summary>
    /// <param name="relativeSegments">The folder names below the profile root.</param>
    public bool ContainsProtectedPath(IReadOnlyList<string> relativeSegments)
    {
        ArgumentNullException.ThrowIfNull(relativeSegments);

        TrieNode[] states = [_trieRoot];
        foreach (string segment in relativeSegments)
        {
            states = Advance(states, segment);
        }

        return states.Any(state => state.HasProtectedBelow);
    }

    private static bool MarkProtectedBelow(TrieNode node)
    {
        bool below = false;
        foreach (TrieNode child in node.Children)
        {
            bool childHasProtected = child.Terminals.Any(match => match.Rule.Kind == RuleKind.Protected);
            below |= MarkProtectedBelow(child) | childHasProtected;
        }

        node.HasProtectedBelow = below;
        return below;
    }

    /// <summary>
    /// Finds the rules of every folder below <paramref name="root"/>, the profile root of the scan target.
    /// Personal folders (<see cref="FolderNode.IsPersonal"/>) are protected with subtree scope.
    /// </summary>
    public RuleMatchMap Match(FolderNode root, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(root);

        var matches = new Dictionary<FolderNode, FolderRuleMatch>();
        var stack = new Stack<Frame>();
        stack.Push(new Frame(root, [_trieRoot], NoMatches));

        while (stack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Frame frame = stack.Pop();

            List<RuleMatch>? direct = null;
            foreach (TrieNode state in frame.States)
            {
                foreach (RuleMatch terminal in state.Terminals)
                {
                    (direct ??= []).Add(terminal);
                }
            }

            if (frame.Node.IsPersonal)
            {
                (direct ??= []).Add(new RuleMatch(PersonalFolderRule, null, IsInherited: false));
            }

            RuleMatch[] inheritedForChildren = frame.Inherited;
            if (direct is not null)
            {
                inheritedForChildren = [.. frame.Inherited, .. direct
                    .Where(match => match.Rule.Scope == RuleScope.Subtree)
                    .Select(match => match with { IsInherited = true })];
                matches[frame.Node] = new FolderRuleMatch(frame.Inherited.Concat(direct));
            }
            else if (frame.Inherited.Length > 0)
            {
                matches[frame.Node] = new FolderRuleMatch(frame.Inherited);
            }

            foreach (FolderNode child in frame.Node.Children)
            {
                stack.Push(new Frame(child, Advance(frame.States, child.Name), inheritedForChildren));
            }
        }

        return new RuleMatchMap(matches);
    }

    private static TrieNode[] Advance(TrieNode[] states, string name)
    {
        List<TrieNode>? next = null;
        foreach (TrieNode state in states)
        {
            if (state.Literals is not null && state.Literals.TryGetValue(name, out TrieNode? literal))
            {
                (next ??= []).Add(literal);
            }

            if (state.Wildcard is { } wildcard)
            {
                (next ??= []).Add(wildcard);
            }
        }

        return next is null ? NoStates : [.. next];
    }

    private readonly record struct Frame(FolderNode Node, TrieNode[] States, RuleMatch[] Inherited);

    private sealed class TrieNode
    {
        public Dictionary<string, TrieNode>? Literals { get; private set; }

        public TrieNode? Wildcard { get; private set; }

        public List<RuleMatch> Terminals { get; } = [];

        /// <summary>True when a protected rule ends somewhere strictly below this node.</summary>
        public bool HasProtectedBelow { get; set; }

        public IEnumerable<TrieNode> Children
        {
            get
            {
                if (Literals is not null)
                {
                    foreach (TrieNode literal in Literals.Values)
                    {
                        yield return literal;
                    }
                }

                if (Wildcard is not null)
                {
                    yield return Wildcard;
                }
            }
        }

        public TrieNode LiteralChild(string name)
        {
            Literals ??= new Dictionary<string, TrieNode>(StringComparer.OrdinalIgnoreCase);
            if (!Literals.TryGetValue(name, out TrieNode? child))
            {
                child = new TrieNode();
                Literals[name] = child;
            }

            return child;
        }

        public TrieNode WildcardChild() => Wildcard ??= new TrieNode();
    }
}
