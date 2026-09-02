using UnoTextPad.Features.Settings;
using UnoTextPad.Tests.TestInfrastructure;
using Xunit;
using static UnoTextPad.Tests.TestInfrastructure.TestCancellation;

namespace UnoTextPad.Tests.Features.Settings;

public class EditorSettingsViewModelTests
{
    private readonly InMemorySettingsRepository _settingsRepository = new();
    private readonly RecordingThemeService _themeService = new();
    private readonly EditorSettingsViewModel _settings;

    public EditorSettingsViewModelTests()
        => _settings = new EditorSettingsViewModel(
            _settingsRepository,
            new StubSystemFontProvider(),
            _themeService);

    [Fact]
    public async Task LoadPreferencesAsync_OnFirstRun_FollowsTheSystemTheme()
    {
        await _settings.LoadPreferencesAsync(Token);

        Assert.False(_settings.IsDarkTheme);
        Assert.Equal([AppTheme.Light], _themeService.AppliedThemes);
    }

    [Fact]
    public async Task LoadPreferencesAsync_WithStoredPreferences_RestoresThem()
    {
        await _settingsRepository.SaveAsync(new EditorSettings
        {
            Theme = AppTheme.Dark,
            FontFamily = "Menlo",
            FontSize = 20,
            WordWrap = true
        }, Token);

        await _settings.LoadPreferencesAsync(Token);

        Assert.True(_settings.IsDarkTheme);
        Assert.Equal(20, _settings.FontSize);
        Assert.True(_settings.IsWordWrapEnabled);
        Assert.Equal([AppTheme.Dark], _themeService.AppliedThemes);
    }

    [Fact]
    public async Task LoadPreferencesAsync_WithAnOutOfRangeFontSize_ClampsIt()
    {
        await _settingsRepository.SaveAsync(new EditorSettings { FontFamily = "Menlo", FontSize = 5000 }, Token);

        await _settings.LoadPreferencesAsync(Token);

        Assert.Equal(EditorSettings.MaximumFontSize, _settings.FontSize);
    }

    [Fact]
    public async Task LoadFontsAsync_ListsTheInstalledFontsAndSelectsTheStoredOne()
    {
        await _settingsRepository.SaveAsync(new EditorSettings { FontFamily = "Menlo" }, Token);
        await _settings.LoadPreferencesAsync(Token);

        await _settings.LoadFontsAsync(Token);

        Assert.Equal(["Courier New", "Menlo"], _settings.AvailableFontFamilies);
        Assert.Equal("Menlo", _settings.SelectedFontFamilyName);
    }

    [Fact]
    public async Task LoadFontsAsync_WhenTheStoredFontIsNotInstalled_FallsBackToADefault()
    {
        await _settingsRepository.SaveAsync(new EditorSettings { FontFamily = "A Font Nobody Has" }, Token);
        await _settings.LoadPreferencesAsync(Token);

        await _settings.LoadFontsAsync(Token);

        Assert.Equal("Courier New", _settings.SelectedFontFamilyName);
    }

    [Fact]
    public async Task ToggleTheme_AppliesAndPersistsTheNewTheme()
    {
        await _settings.LoadPreferencesAsync(Token);
        await _settings.LoadFontsAsync(Token);

        _settings.ToggleTheme();

        Assert.True(_settings.IsDarkTheme);
        Assert.Equal([AppTheme.Light, AppTheme.Dark], _themeService.AppliedThemes);
        Assert.Equal(AppTheme.Dark, (await _settingsRepository.LoadAsync(Token)).Theme);
    }

    [Fact]
    public async Task FontSize_WhenSetToNotANumber_KeepsTheLastValidSize()
    {
        await _settings.LoadPreferencesAsync(Token);
        await _settings.LoadFontsAsync(Token);
        _settings.FontSize = 18;

        _settings.FontSize = double.NaN;

        Assert.Equal(18, _settings.FontSize);
    }

    [Fact]
    public async Task FontSize_WhenSetAboveTheMaximum_IsClamped()
    {
        await _settings.LoadPreferencesAsync(Token);
        await _settings.LoadFontsAsync(Token);

        _settings.FontSize = 500;

        Assert.Equal(EditorSettings.MaximumFontSize, _settings.FontSize);
    }

    [Fact]
    public async Task ChangingAPreference_IsWrittenBackToStorage()
    {
        await _settings.LoadPreferencesAsync(Token);
        await _settings.LoadFontsAsync(Token);

        _settings.IsWordWrapEnabled = true;
        _settings.FontSize = 22;

        var stored = await _settingsRepository.LoadAsync(Token);
        Assert.True(stored.WordWrap);
        Assert.Equal(22, stored.FontSize);
        Assert.Equal("Courier New", stored.FontFamily);
    }
}
