namespace LeftoverScout.Core.Scanning;

/// <summary>
/// The single place for the reparse point and cloud file rules of the scanner (task T-002, section 2).
/// Constants: "File Attribute Constants" and "Reparse Point Tags" on Microsoft Learn.
/// </summary>
public static class EntryClassifier
{
    /// <summary>FILE_ATTRIBUTE_DIRECTORY.</summary>
    public const uint AttributeDirectory = 0x00000010;

    /// <summary>FILE_ATTRIBUTE_REPARSE_POINT.</summary>
    public const uint AttributeReparsePoint = 0x00000400;

    /// <summary>FILE_ATTRIBUTE_OFFLINE.</summary>
    public const uint AttributeOffline = 0x00001000;

    /// <summary>FILE_ATTRIBUTE_RECALL_ON_OPEN.</summary>
    public const uint AttributeRecallOnOpen = 0x00040000;

    /// <summary>FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS.</summary>
    public const uint AttributeRecallOnDataAccess = 0x00400000;

    /// <summary>Bit that marks a reparse tag as a name surrogate (junction, symlink, ...).</summary>
    public const uint ReparseTagNameSurrogateBit = 0x20000000;

    /// <summary>IO_REPARSE_TAG_MOUNT_POINT (junction).</summary>
    public const uint ReparseTagMountPoint = 0xA0000003;

    /// <summary>IO_REPARSE_TAG_SYMLINK.</summary>
    public const uint ReparseTagSymlink = 0xA000000C;

    /// <summary>IO_REPARSE_TAG_CLOUD (base value of the cloud family, which differs in bits 12-15).</summary>
    public const uint ReparseTagCloud = 0x9000001A;

    /// <summary>IO_REPARSE_TAG_APPEXECLINK (app execution alias).</summary>
    public const uint ReparseTagAppExecLink = 0x8000001B;

    /// <summary>Returns true when the tag is in the IO_REPARSE_TAG_CLOUD family.</summary>
    public static bool IsCloudTag(uint reparseTag) => (reparseTag & 0xFFFF0FFF) == ReparseTagCloud;

    /// <summary>Returns true when the tag has the name-surrogate bit.</summary>
    public static bool IsNameSurrogate(uint reparseTag) => (reparseTag & ReparseTagNameSurrogateBit) != 0;

    /// <summary>Decides how the scanner treats <paramref name="entry"/>.</summary>
    public static EntryKind Classify(DirectoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        bool isReparsePoint = (entry.Attributes & AttributeReparsePoint) != 0;

        // Rule 1: name-surrogate reparse points are never followed or counted. The tag is only meaningful
        // when the reparse point attribute is set.
        if (isReparsePoint && IsNameSurrogate(entry.ReparseTag))
        {
            return EntryKind.SkippedLink;
        }

        if (entry.IsDirectory)
        {
            // Rule 4: a folder that is not populated locally must not be enumerated.
            return (entry.Attributes & AttributeRecallOnOpen) != 0
                ? EntryKind.CloudFolderNotScanned
                : EntryKind.Directory;
        }

        // Rule 3: online-only files. Rules 2 and 5: cloud placeholders and other non-surrogate reparse
        // files are plain files measured by metadata only.
        return (entry.Attributes & (AttributeRecallOnDataAccess | AttributeOffline)) != 0
            ? EntryKind.CloudOnlyFile
            : EntryKind.File;
    }
}
