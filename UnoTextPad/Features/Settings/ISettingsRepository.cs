namespace UnoTextPad.Features.Settings;

/// <summary>Stores the user's editor preferences between runs.</summary>
public interface ISettingsRepository
{
    /// <summary>Returns the stored settings, or freshly defaulted settings on first run.</summary>
    Task<EditorSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(EditorSettings settings, CancellationToken cancellationToken = default);
}
