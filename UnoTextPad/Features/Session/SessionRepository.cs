using System.Text;
using UnoTextPad.Infrastructure.Storage;

namespace UnoTextPad.Features.Session;

/// <inheritdoc cref="ISessionRepository"/>
public sealed class SessionRepository : ISessionRepository
{
    private static readonly UTF8Encoding BackupEncoding = new(encoderShouldEmitUTF8Identifier: false);

    private readonly IAppDataPathProvider _pathProvider;
    private readonly IJsonFileStore _jsonFileStore;

    public SessionRepository(IAppDataPathProvider pathProvider, IJsonFileStore jsonFileStore)
    {
        _pathProvider = pathProvider;
        _jsonFileStore = jsonFileStore;
    }

    public Task<SessionSnapshot?> LoadAsync(CancellationToken cancellationToken = default)
        => _jsonFileStore.ReadAsync<SessionSnapshot>(_pathProvider.SessionFilePath, cancellationToken);

    public Task SaveAsync(SessionSnapshot snapshot, CancellationToken cancellationToken = default)
        => _jsonFileStore.WriteAsync(_pathProvider.SessionFilePath, snapshot, cancellationToken);

    public async Task<string?> LoadBackupAsync(string documentId, CancellationToken cancellationToken = default)
    {
        var backupFilePath = _pathProvider.GetBackupFilePath(documentId);

        if (!File.Exists(backupFilePath))
        {
            return null;
        }

        try
        {
            return await File.ReadAllTextAsync(backupFilePath, BackupEncoding, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (IOException)
        {
            return null;
        }
    }

    public Task SaveBackupAsync(string documentId, string text, CancellationToken cancellationToken = default)
        => File.WriteAllTextAsync(
            _pathProvider.GetBackupFilePath(documentId),
            text,
            BackupEncoding,
            cancellationToken);

    public void DeleteBackup(string documentId) => TryDelete(_pathProvider.GetBackupFilePath(documentId));

    public void DeleteBackupsExcept(IReadOnlyCollection<string> documentIdsToKeep)
    {
        var backupsFolderPath = Path.GetDirectoryName(_pathProvider.GetBackupFilePath("placeholder"));

        if (backupsFolderPath is null || !Directory.Exists(backupsFolderPath))
        {
            return;
        }

        foreach (var backupFilePath in Directory.EnumerateFiles(backupsFolderPath, "*.txt"))
        {
            var documentId = Path.GetFileNameWithoutExtension(backupFilePath);

            if (!documentIdsToKeep.Contains(documentId))
            {
                TryDelete(backupFilePath);
            }
        }
    }

    private static void TryDelete(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (IOException)
        {
            // A backup that cannot be deleted is harmless; it is overwritten or cleaned up later.
        }
    }
}
