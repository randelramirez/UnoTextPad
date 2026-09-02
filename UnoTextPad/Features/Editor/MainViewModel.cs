using System.Collections.ObjectModel;
using System.ComponentModel;
using UnoTextPad.Features.Documents;
using UnoTextPad.Features.Session;
using UnoTextPad.Features.Settings;

namespace UnoTextPad.Features.Editor;

/// <summary>
/// Drives the editor window: the open tabs, the commands on the toolbar, the status bar and
/// the debounced save of the session.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    /// <summary>
    /// How long editing has to pause before the session is written. Editing must never wait
    /// on the disk, so the backup happens once the user stops typing.
    /// </summary>
    private static readonly TimeSpan SessionSaveDelay = TimeSpan.FromSeconds(1.5);

    private readonly IDocumentSessionCoordinator _sessionCoordinator;
    private readonly ITextFileService _textFileService;
    private readonly IFilePickerService _filePickerService;
    private readonly IDialogService _dialogService;
    private readonly DispatcherTimer _sessionSaveTimer = new() { Interval = SessionSaveDelay };

    private CaretLocation _caretLocation = new(1, 1);
    private bool _isRestoringSession;

    public MainViewModel(
        EditorSettingsViewModel settings,
        IDocumentSessionCoordinator sessionCoordinator,
        ITextFileService textFileService,
        IFilePickerService filePickerService,
        IDialogService dialogService)
    {
        Settings = settings;
        _sessionCoordinator = sessionCoordinator;
        _textFileService = textFileService;
        _filePickerService = filePickerService;
        _dialogService = dialogService;

        _sessionSaveTimer.Tick += OnSessionSaveTimerTick;

        // Covers adding, closing and dragging tabs into a different order.
        Documents.CollectionChanged += (sender, changedArgs) => RequestSessionSave();
    }

    public EditorSettingsViewModel Settings { get; }

    public ObservableCollection<DocumentViewModel> Documents { get; } = [];

    [ObservableProperty]
    public partial DocumentViewModel? SelectedDocument { get; set; }

    public string WindowTitle => SelectedDocument is null
        ? "UnoTextPad"
        : $"{SelectedDocument.DisplayTitle} — UnoTextPad";

    public string StatusFilePath => SelectedDocument?.FilePath ?? "Not saved";

    public string StatusCaret => $"Ln {_caretLocation.Line}, Col {_caretLocation.Column}";

    public string StatusEncoding => SelectedDocument is null ? string.Empty : Describe(SelectedDocument.Encoding);

    public string StatusLineEnding => SelectedDocument is null ? string.Empty : Describe(SelectedDocument.LineEnding);

    /// <summary>
    /// Applies the stored preferences. Call this before the window is shown so that it never
    /// appears in the wrong theme.
    /// </summary>
    public Task LoadPreferencesAsync(CancellationToken cancellationToken = default)
        => Settings.LoadPreferencesAsync(cancellationToken);

    /// <summary>
    /// Loads the installed fonts and restores the previous session, opening a single empty
    /// tab on first run. Call this once the window is visible.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await Settings.LoadFontsAsync(cancellationToken).ConfigureAwait(true);

        _isRestoringSession = true;

        try
        {
            var restoredSession = await _sessionCoordinator
                .RestoreAsync(Settings, cancellationToken).ConfigureAwait(true);

            foreach (var document in restoredSession.Documents)
            {
                AddDocument(document);
            }

            SelectedDocument = restoredSession.ActiveDocumentIndex >= 0
                && restoredSession.ActiveDocumentIndex < Documents.Count
                    ? Documents[restoredSession.ActiveDocumentIndex]
                    : Documents.FirstOrDefault();
        }
        finally
        {
            _isRestoringSession = false;
        }

        if (Documents.Count == 0)
        {
            CreateDocument();
        }
        else
        {
            // What was restored can differ from what was stored, because tabs whose file has
            // since been deleted are dropped. Write the reconciled set back.
            RequestSessionSave();
        }
    }

    /// <summary>Writes the session immediately, for use when the window is closing.</summary>
    public Task SaveSessionNowAsync()
    {
        _sessionSaveTimer.Stop();
        return SaveSessionAsync();
    }

    /// <summary>Records the caret offset of the active document and refreshes the status bar.</summary>
    public void UpdateCaretPosition(int caretIndex)
    {
        if (SelectedDocument is not { } document)
        {
            return;
        }

        document.CaretPosition = caretIndex;
        _caretLocation = CaretLocation.Calculate(document.Content, caretIndex);
        OnPropertyChanged(nameof(StatusCaret));
    }

    [RelayCommand]
    private void NewDocument() => CreateDocument();

    [RelayCommand]
    private async Task OpenFilesAsync()
    {
        var filePaths = await _filePickerService.PickFilesToOpenAsync().ConfigureAwait(true);

        foreach (var filePath in filePaths)
        {
            await OpenFileAsync(filePath).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private Task SaveAsync() => SaveDocumentAsync(SelectedDocument);

    [RelayCommand]
    private Task SaveAsAsync() => SaveDocumentAsAsync(SelectedDocument);

    [RelayCommand]
    private Task CloseDocumentAsync(DocumentViewModel? document) => CloseAsync(document ?? SelectedDocument);

    [RelayCommand]
    private void ToggleTheme() => Settings.ToggleTheme();

    /// <summary>Opens a file, or selects the tab already showing it.</summary>
    public async Task OpenFileAsync(string filePath)
    {
        var alreadyOpenDocument = Documents.FirstOrDefault(
            document => string.Equals(document.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (alreadyOpenDocument is not null)
        {
            SelectedDocument = alreadyOpenDocument;
            return;
        }

        try
        {
            var fileContent = await _textFileService.ReadAsync(filePath).ConfigureAwait(true);
            var document = new DocumentViewModel(CreateDocumentId(), Path.GetFileName(filePath), Settings);

            document.AttachToFile(filePath, fileContent.Encoding, fileContent.LineEnding);
            document.LoadContent(fileContent.Text);

            ReplaceUntouchedFirstTab();
            AddDocument(document);
            SelectedDocument = document;
            RequestSessionSave();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await _dialogService
                .ShowMessageAsync("Could not open file", $"{Path.GetFileName(filePath)}: {exception.Message}")
                .ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Saves the document, returning <c>false</c> when the user cancelled or the write failed
    /// so that callers such as tab closing can stop.
    /// </summary>
    public async Task<bool> SaveDocumentAsync(DocumentViewModel? document)
    {
        if (document is null)
        {
            return false;
        }

        if (document.FilePath is null)
        {
            return await SaveDocumentAsAsync(document).ConfigureAwait(true);
        }

        return await WriteDocumentAsync(document, document.FilePath).ConfigureAwait(true);
    }

    public async Task<bool> SaveDocumentAsAsync(DocumentViewModel? document)
    {
        if (document is null)
        {
            return false;
        }

        var suggestedFileName = document.FilePath is null ? $"{document.FileName}.txt" : document.FileName;
        var filePath = await _filePickerService.PickFileToSaveAsync(suggestedFileName).ConfigureAwait(true);

        return filePath is not null && await WriteDocumentAsync(document, filePath).ConfigureAwait(true);
    }

    /// <summary>
    /// Closes a tab, prompting first when it holds unsaved work. The window always keeps at
    /// least one tab so the editor is never empty.
    /// </summary>
    public async Task CloseAsync(DocumentViewModel? document)
    {
        if (document is null)
        {
            return;
        }

        if (document.HasUnsavedChanges)
        {
            var choice = await _dialogService.AskToSaveChangesAsync(document.FileName).ConfigureAwait(true);

            if (choice == SaveChangesChoice.Cancel)
            {
                return;
            }

            if (choice == SaveChangesChoice.Save && !await SaveDocumentAsync(document).ConfigureAwait(true))
            {
                return;
            }
        }

        var closedIndex = Documents.IndexOf(document);
        RemoveDocument(document);

        if (Documents.Count == 0)
        {
            CreateDocument();
        }
        else
        {
            SelectedDocument = Documents[Math.Min(closedIndex, Documents.Count - 1)];
        }
    }

    private async Task<bool> WriteDocumentAsync(DocumentViewModel document, string filePath)
    {
        try
        {
            await _textFileService.WriteAsync(filePath, document.ToFileContent()).ConfigureAwait(true);

            document.AttachToFile(filePath, document.Encoding, document.LineEnding);
            document.HasUnsavedChanges = false;

            RefreshDocumentStatus();
            RequestSessionSave();

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await _dialogService
                .ShowMessageAsync("Could not save file", $"{Path.GetFileName(filePath)}: {exception.Message}")
                .ConfigureAwait(true);

            return false;
        }
    }

    private void CreateDocument()
    {
        var document = new DocumentViewModel(CreateDocumentId(), CreateUntitledFileName(), Settings);

        AddDocument(document);
        SelectedDocument = document;
        RequestSessionSave();
    }

    private void AddDocument(DocumentViewModel document)
    {
        document.Edited += OnDocumentEdited;
        document.PropertyChanged += OnDocumentPropertyChanged;
        Documents.Add(document);
    }

    private void RemoveDocument(DocumentViewModel document)
    {
        document.Edited -= OnDocumentEdited;
        document.PropertyChanged -= OnDocumentPropertyChanged;
        Documents.Remove(document);
    }

    /// <summary>
    /// Opening a file into a pristine, never edited "Untitled 1" tab replaces it, which is
    /// what a text editor is expected to do on the first open.
    /// </summary>
    private void ReplaceUntouchedFirstTab()
    {
        if (Documents is [{ FilePath: null, HasUnsavedChanges: false, Content.Length: 0 } onlyDocument])
        {
            RemoveDocument(onlyDocument);
        }
    }

    private string CreateUntitledFileName()
    {
        var usedNames = Documents
            .Select(document => document.FileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var number = 1;

        while (usedNames.Contains($"Untitled {number}"))
        {
            number++;
        }

        return $"Untitled {number}";
    }

    private static string CreateDocumentId() => Guid.NewGuid().ToString("N");

    private void OnDocumentEdited(object? sender, EventArgs args) => RequestSessionSave();

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (ReferenceEquals(sender, SelectedDocument) && args.PropertyName == nameof(DocumentViewModel.DisplayTitle))
        {
            OnPropertyChanged(nameof(WindowTitle));
        }
    }

    partial void OnSelectedDocumentChanged(DocumentViewModel? value)
    {
        _caretLocation = CaretLocation.Calculate(value?.Content ?? string.Empty, value?.CaretPosition ?? 0);
        RefreshDocumentStatus();

        if (!_isRestoringSession)
        {
            RequestSessionSave();
        }
    }

    private void RefreshDocumentStatus()
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(StatusFilePath));
        OnPropertyChanged(nameof(StatusCaret));
        OnPropertyChanged(nameof(StatusEncoding));
        OnPropertyChanged(nameof(StatusLineEnding));
    }

    /// <summary>Restarts the idle timer so that a burst of typing results in a single save.</summary>
    private void RequestSessionSave()
    {
        if (_isRestoringSession)
        {
            return;
        }

        _sessionSaveTimer.Stop();
        _sessionSaveTimer.Start();
    }

    private void OnSessionSaveTimerTick(object? sender, object args)
    {
        _sessionSaveTimer.Stop();
        _ = SaveSessionAsync();
    }

    private Task SaveSessionAsync()
    {
        var activeDocumentIndex = SelectedDocument is null ? -1 : Documents.IndexOf(SelectedDocument);

        return _sessionCoordinator.SaveAsync(Documents.ToArray(), activeDocumentIndex);
    }

    private static string Describe(TextEncodingKind encoding) => encoding switch
    {
        TextEncodingKind.Utf8Bom => "UTF-8 BOM",
        TextEncodingKind.Utf16Le => "UTF-16 LE",
        TextEncodingKind.Utf16Be => "UTF-16 BE",
        _ => "UTF-8"
    };

    private static string Describe(LineEndingStyle lineEnding) => lineEnding switch
    {
        LineEndingStyle.CrLf => "CRLF",
        LineEndingStyle.Cr => "CR",
        _ => "LF"
    };
}
