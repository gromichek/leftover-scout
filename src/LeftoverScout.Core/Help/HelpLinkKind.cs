namespace LeftoverScout.Core.Help;

/// <summary>
/// What a link inside a help topic does when clicked.
/// </summary>
public enum HelpLinkKind
{
    /// <summary>The link is not supported and is ignored.</summary>
    Ignored,

    /// <summary>The link points to another help topic; it opens inside the Help page.</summary>
    Topic,

    /// <summary>The link is an <c>https://</c> address; it opens in the browser.</summary>
    External,
}
