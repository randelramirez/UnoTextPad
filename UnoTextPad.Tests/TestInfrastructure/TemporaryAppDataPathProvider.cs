using UnoTextPad.Infrastructure.Storage;

namespace UnoTextPad.Tests.TestInfrastructure;

/// <summary>
/// Points the repositories at a throwaway folder so tests never touch the real application
/// data of the machine they run on.
/// </summary>
internal sealed class TemporaryAppDataPathProvider : IAppDataPathProvider, IDisposable
{
    private readonly string _rootFolderPath;

    public TemporaryAppDataPathProvider()
    {
        _rootFolderPath = Path.Combine(Path.GetTempPath(), $"unotextpad-tests-{Guid.NewGuid():N}");
        SettingsFilePath = Path.Combine(_rootFolderPath, "settings.json");
        SessionFilePath = Path.Combine(_rootFolderPath, "session.json");
        EnsureDirectoriesExist();
    }

    public string SettingsFilePath { get; }

    public string SessionFilePath { get; }

    public string GetBackupFilePath(string documentId)
        => Path.Combine(_rootFolderPath, "Backups", $"{documentId}.txt");

    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(_rootFolderPath);
        Directory.CreateDirectory(Path.Combine(_rootFolderPath, "Backups"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootFolderPath))
        {
            Directory.Delete(_rootFolderPath, recursive: true);
        }
    }
}
