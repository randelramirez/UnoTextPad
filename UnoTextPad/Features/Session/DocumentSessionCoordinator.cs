using UnoTextPad.Features.Documents;
using UnoTextPad.Features.Settings;

namespace UnoTextPad.Features.Session;

/// <summary>
/// Restores and stores the open tabs. Documents without unsaved changes are re-read from
/// disk, so only edited buffers cost a backup file — the same trade-off Notepad++ makes.
/// </summary>
public sealed class DocumentSessionCoordinator : IDocumentSessionCoordinator
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ITextFileService _textFileService;

    public DocumentSessionCoordinator(ISessionRepository sessionRepository, ITextFileService textFileService)
    {
        _sessionRepository = sessionRepository;
        _textFileService = textFileService;
    }

    public async Task<RestoredSession> RestoreAsync(
        EditorSettingsViewModel settings,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _sessionRepository.LoadAsync(cancellationToken).ConfigureAwait(true);

        if (snapshot is null || snapshot.Documents.Count == 0)
        {
            return new RestoredSession([], -1);
        }

        var restoredDocuments = new List<DocumentViewModel>(snapshot.Documents.Count);
        var restoredActiveIndex = -1;

        for (var index = 0; index < snapshot.Documents.Count; index++)
        {
            var documentSnapshot = snapshot.Documents[index];
            var document = await RestoreDocumentAsync(documentSnapshot, settings, cancellationToken)
                .ConfigureAwait(true);

            if (document is null)
            {
                continue;
            }

            if (index == snapshot.ActiveDocumentIndex)
            {
                restoredActiveIndex = restoredDocuments.Count;
            }

            restoredDocuments.Add(document);
        }

        if (restoredActiveIndex < 0 && restoredDocuments.Count > 0)
        {
            restoredActiveIndex = 0;
        }

        return new RestoredSession(restoredDocuments, restoredActiveIndex);
    }

    public async Task SaveAsync(
        IReadOnlyList<DocumentViewModel> documents,
        int activeDocumentIndex,
        CancellationToken cancellationToken = default)
    {
        var snapshot = new SessionSnapshot { ActiveDocumentIndex = activeDocumentIndex };

        foreach (var document in documents)
        {
            snapshot.Documents.Add(new DocumentSnapshot
            {
                Id = document.Id,
                FilePath = document.FilePath,
                DisplayName = document.FileName,
                HasUnsavedChanges = document.HasUnsavedChanges,
                CaretPosition = document.CaretPosition,
                Encoding = document.Encoding,
                LineEnding = document.LineEnding
            });

            // Only unsaved work needs a backup; a clean tab is cheaper to re-read from disk.
            if (document.HasUnsavedChanges || document.FilePath is null)
            {
                await _sessionRepository.SaveBackupAsync(document.Id, document.Content, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                _sessionRepository.DeleteBackup(document.Id);
            }
        }

        _sessionRepository.DeleteBackupsExcept(documents.Select(document => document.Id).ToArray());

        await _sessionRepository.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DocumentViewModel?> RestoreDocumentAsync(
        DocumentSnapshot snapshot,
        EditorSettingsViewModel settings,
        CancellationToken cancellationToken)
    {
        var hasBackup = snapshot.HasUnsavedChanges || snapshot.FilePath is null;
        var backupText = hasBackup
            ? await _sessionRepository.LoadBackupAsync(snapshot.Id, cancellationToken).ConfigureAwait(true)
            : null;

        var fileExists = snapshot.FilePath is not null && File.Exists(snapshot.FilePath);

        // A tab is only worth restoring if its text can still be found somewhere.
        if (backupText is null && !fileExists)
        {
            _sessionRepository.DeleteBackup(snapshot.Id);
            return null;
        }

        var displayName = string.IsNullOrEmpty(snapshot.DisplayName) ? "Untitled" : snapshot.DisplayName;
        var document = new DocumentViewModel(snapshot.Id, displayName, settings);

        if (fileExists)
        {
            document.AttachToFile(snapshot.FilePath!, snapshot.Encoding, snapshot.LineEnding);
        }
        else
        {
            document.RestoreFileFormat(snapshot.Encoding, snapshot.LineEnding);
        }

        if (backupText is not null)
        {
            document.LoadContent(backupText);
            document.HasUnsavedChanges = snapshot.HasUnsavedChanges;
        }
        else
        {
            var fileContent = await _textFileService
                .ReadAsync(snapshot.FilePath!, cancellationToken).ConfigureAwait(true);

            document.AttachToFile(snapshot.FilePath!, fileContent.Encoding, fileContent.LineEnding);
            document.LoadContent(fileContent.Text);
        }

        document.CaretPosition = snapshot.CaretPosition;

        return document;
    }
}
