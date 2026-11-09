namespace LeftoverScout.Core.Rules;

/// <summary>How far a rule reaches below the folder its path pattern names.</summary>
public enum RuleScope
{
    /// <summary>Only the named folder itself.</summary>
    Folder,

    /// <summary>The named folder and everything below it.</summary>
    Subtree,
}
