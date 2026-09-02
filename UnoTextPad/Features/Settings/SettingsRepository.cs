using UnoTextPad.Infrastructure.Storage;

namespace UnoTextPad.Features.Settings;

/// <inheritdoc cref="ISettingsRepository"/>
public sealed class SettingsRepository : ISettingsRepository
{
    private readonly IAppDataPathProvider _pathProvider;
    private readonly IJsonFileStore _jsonFileStore;

    public SettingsRepository(IAppDataPathProvider pathProvider, IJsonFileStore jsonFileStore)
    {
        _pathProvider = pathProvider;
        _jsonFileStore = jsonFileStore;
    }

    public async Task<EditorSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _jsonFileStore
            .ReadAsync<EditorSettings>(_pathProvider.SettingsFilePath, cancellationToken)
            .ConfigureAwait(false);

        return settings ?? new EditorSettings();
    }

    public Task SaveAsync(EditorSettings settings, CancellationToken cancellationToken = default)
        => _jsonFileStore.WriteAsync(_pathProvider.SettingsFilePath, settings, cancellationToken);
}
