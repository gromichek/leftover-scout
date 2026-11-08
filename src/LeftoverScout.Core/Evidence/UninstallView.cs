namespace LeftoverScout.Core.Evidence;

/// <summary>Registry view (64-bit or 32-bit) used to read an Uninstall key.</summary>
public enum UninstallView
{
    /// <summary>The 64-bit registry view.</summary>
    Registry64,

    /// <summary>The 32-bit registry view (Wow6432Node).</summary>
    Registry32,
}
