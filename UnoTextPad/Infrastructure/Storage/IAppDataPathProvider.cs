namespace UnoTextPad.Infrastructure.Storage;

/// <summary>
/// Supplies the locations UnoTextPad stores its own state in. Isolating this keeps the
/// repositories free of any knowledge about where the platform puts application data.
/// </summary>
public interface IAppDataPathProvider
{
    /// <summary>Full path of the JSON file holding <see cref="Models.EditorSettings"/>.</summary>
    string SettingsFilePath { get; }

    /// <summary>Full path of the JSON file holding the <see cref="Models.SessionSnapshot"/>.</summary>
    string SessionFilePath { get; }

    /// <summary>Full path of the backup holding the unsaved text of one document.</summary>
    string GetBackupFilePath(string documentId);

    /// <summary>Creates the directories the other paths live in, if they do not exist yet.</summary>
    void EnsureDirectoriesExist();
}
