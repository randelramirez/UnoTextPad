using UnoTextPad.Features.Documents;

namespace UnoTextPad.Features.Session;

/// <summary>
/// The persisted description of a single open tab. Text is only stored separately, in a
/// backup file, when the tab has unsaved changes; otherwise it is re-read from disk.
/// </summary>
public sealed class DocumentSnapshot
{
    /// <summary>Stable identifier, also used as the backup file name.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The file on disk, or <c>null</c> for a tab that was never saved.</summary>
    public string? FilePath { get; set; }

    /// <summary>The tab caption, which matters for never-saved tabs such as "Untitled 2".</summary>
    public string DisplayName { get; set; } = string.Empty;

    public bool HasUnsavedChanges { get; set; }

    public int CaretPosition { get; set; }

    public TextEncodingKind Encoding { get; set; } = TextEncodingKind.Utf8;

    public LineEndingStyle LineEnding { get; set; } = LineEndingStyle.Lf;
}
