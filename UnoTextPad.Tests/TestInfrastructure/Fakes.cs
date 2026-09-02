using UnoTextPad.Features.Settings;

namespace UnoTextPad.Tests.TestInfrastructure;

/// <summary>Keeps settings in memory so tests never touch the real preferences file.</summary>
internal sealed class InMemorySettingsRepository : ISettingsRepository
{
    private EditorSettings _settings = new();

    public Task<EditorSettings> LoadAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_settings);

    public Task SaveAsync(EditorSettings settings, CancellationToken cancellationToken = default)
    {
        _settings = settings;
        return Task.CompletedTask;
    }
}

/// <summary>Returns a fixed font list so tests do not depend on the machine's installed fonts.</summary>
internal sealed class StubSystemFontProvider : ISystemFontProvider
{
    public Task<IReadOnlyList<string>> GetFontFamiliesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>(["Courier New", "Menlo"]);

    public string ResolveDefaultFontFamily(IReadOnlyList<string> installedFontFamilies) => "Courier New";
}

/// <summary>Records the themes it was asked to apply, without needing a window.</summary>
internal sealed class RecordingThemeService : IThemeService
{
    public List<AppTheme> AppliedThemes { get; } = [];

    public void Apply(AppTheme theme) => AppliedThemes.Add(theme);

    public AppTheme GetSystemTheme() => AppTheme.Light;
}
