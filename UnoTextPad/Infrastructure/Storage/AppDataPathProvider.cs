using Windows.Storage;

namespace UnoTextPad.Infrastructure.Storage;

/// <summary>
/// Resolves storage paths under the per-user application data folder that Uno Platform
/// maps to the right location on Windows, macOS and Linux.
/// </summary>
public sealed class AppDataPathProvider : IAppDataPathProvider
{
    private const string BackupsFolderName = "Backups";

    private readonly string _rootFolderPath;

    public AppDataPathProvider()
    {
        _rootFolderPath = ApplicationData.Current.LocalFolder.Path;
        SettingsFilePath = Path.Combine(_rootFolderPath, "settings.json");
        SessionFilePath = Path.Combine(_rootFolderPath, "session.json");
    }

    public string SettingsFilePath { get; }

    public string SessionFilePath { get; }

    public string GetBackupFilePath(string documentId)
        => Path.Combine(_rootFolderPath, BackupsFolderName, $"{documentId}.txt");

    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(_rootFolderPath);
        Directory.CreateDirectory(Path.Combine(_rootFolderPath, BackupsFolderName));
    }
}
