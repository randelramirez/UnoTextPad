using CommunityToolkit.Mvvm.ComponentModel;
using UnoTextPad.Features.Settings;

namespace UnoTextPad.Features.Documents;

/// <summary>
/// One open tab: the text being edited plus everything needed to save it back to the same
/// file, in the same encoding, with the same line endings.
/// </summary>
public sealed partial class DocumentViewModel : ObservableObject
{
    /// <summary>Suppresses the dirty flag while text is being loaded rather than typed.</summary>
    private bool _isLoadingContent;

    public DocumentViewModel(string id, string fileName, EditorSettingsViewModel settings)
    {
        Id = id;
        Settings = settings;
        FileName = fileName;
    }

    /// <summary>Stable identifier used to match the tab to its backup file across restarts.</summary>
    public string Id { get; }

    /// <summary>The shared appearance settings, bound to by the text box inside the tab template.</summary>
    public EditorSettingsViewModel Settings { get; }

    /// <summary>Raised on every edit so the session can be backed up shortly afterwards.</summary>
    public event EventHandler? Edited;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    public partial string FileName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    public partial bool HasUnsavedChanges { get; set; }

    [ObservableProperty]
    public partial string Content { get; set; } = string.Empty;

    /// <summary>The file this tab was opened from, or <c>null</c> for a tab never saved to disk.</summary>
    public string? FilePath { get; private set; }

    public TextEncodingKind Encoding { get; private set; } = TextEncodingKind.Utf8;

    public LineEndingStyle LineEnding { get; private set; } =
        OperatingSystem.IsWindows() ? LineEndingStyle.CrLf : LineEndingStyle.Lf;

    /// <summary>Where the caret sat when the tab was last active, restored with the session.</summary>
    public int CaretPosition { get; set; }

    /// <summary>The tab caption, marked with a bullet while there are unsaved changes.</summary>
    public string DisplayTitle => HasUnsavedChanges ? $"{FileName} •" : FileName;

    /// <summary>Replaces the text without marking the document as edited.</summary>
    public void LoadContent(string content)
    {
        _isLoadingContent = true;

        try
        {
            Content = content;
        }
        finally
        {
            _isLoadingContent = false;
        }
    }

    /// <summary>Records the file this document reads from and writes back to.</summary>
    public void AttachToFile(string filePath, TextEncodingKind encoding, LineEndingStyle lineEnding)
    {
        FilePath = filePath;
        Encoding = encoding;
        LineEnding = lineEnding;
        FileName = Path.GetFileName(filePath);
    }

    /// <summary>Restores the encoding and line endings of a document reloaded from a session.</summary>
    public void RestoreFileFormat(TextEncodingKind encoding, LineEndingStyle lineEnding)
    {
        Encoding = encoding;
        LineEnding = lineEnding;
    }

    public TextFileContent ToFileContent() => new(Content, Encoding, LineEnding);

    partial void OnContentChanged(string value)
    {
        if (_isLoadingContent)
        {
            return;
        }

        HasUnsavedChanges = true;
        Edited?.Invoke(this, EventArgs.Empty);
    }
}
