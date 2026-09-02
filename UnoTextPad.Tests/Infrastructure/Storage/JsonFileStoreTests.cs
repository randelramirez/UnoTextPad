namespace UnoTextPad.Tests.Infrastructure.Storage;

public class JsonFileStoreTests : IDisposable
{
    private readonly TemporaryAppDataPathProvider _pathProvider = new();
    private readonly JsonFileStore _store = new();

    public void Dispose() => _pathProvider.Dispose();

    [Fact]
    public async Task ReadAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        var settings = await _store.ReadAsync<EditorSettings>(_pathProvider.SettingsFilePath, Token);

        Assert.Null(settings);
    }

    [Fact]
    public async Task ReadAsync_WhenFileIsCorrupt_ReturnsNullInsteadOfThrowing()
    {
        await File.WriteAllTextAsync(_pathProvider.SettingsFilePath, "{ this is not json", Token);

        var settings = await _store.ReadAsync<EditorSettings>(_pathProvider.SettingsFilePath, Token);

        Assert.Null(settings);
    }

    [Fact]
    public async Task WriteAsync_ThenReadAsync_RoundTripsEveryValue()
    {
        var original = new EditorSettings
        {
            Theme = AppTheme.Dark,
            FontFamily = "Menlo",
            FontSize = 18,
            WordWrap = true
        };

        await _store.WriteAsync(_pathProvider.SettingsFilePath, original, Token);
        var reloaded = await _store.ReadAsync<EditorSettings>(_pathProvider.SettingsFilePath, Token);

        Assert.Equivalent(original, reloaded, strict: true);
    }

    [Fact]
    public async Task WriteAsync_LeavesNoTemporaryFileBehind()
    {
        await _store.WriteAsync(_pathProvider.SessionFilePath, new SessionSnapshot(), Token);

        Assert.False(File.Exists(_pathProvider.SessionFilePath + ".tmp"));
    }

    [Fact]
    public async Task WriteAsync_OverwritesAnExistingFile()
    {
        await _store.WriteAsync(_pathProvider.SettingsFilePath, new EditorSettings { FontSize = 10 }, Token);
        await _store.WriteAsync(_pathProvider.SettingsFilePath, new EditorSettings { FontSize = 24 }, Token);

        var reloaded = await _store.ReadAsync<EditorSettings>(_pathProvider.SettingsFilePath, Token);

        Assert.Equal(24, reloaded!.FontSize);
    }
}
