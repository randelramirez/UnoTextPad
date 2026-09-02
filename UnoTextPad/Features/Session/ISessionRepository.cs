namespace UnoTextPad.Features.Session;

/// <summary>
/// Stores the set of open tabs between runs, together with a backup of the text of every
/// tab that has unsaved changes.
/// </summary>
public interface ISessionRepository
{
    Task<SessionSnapshot?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SessionSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>Returns the backed up text of a document, or <c>null</c> when there is none.</summary>
    Task<string?> LoadBackupAsync(string documentId, CancellationToken cancellationToken = default);

    Task SaveBackupAsync(string documentId, string text, CancellationToken cancellationToken = default);

    void DeleteBackup(string documentId);

    /// <summary>Removes backups that no longer belong to any open document.</summary>
    void DeleteBackupsExcept(IReadOnlyCollection<string> documentIdsToKeep);
}
