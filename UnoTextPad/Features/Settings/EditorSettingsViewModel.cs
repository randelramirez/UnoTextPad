using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media;

namespace UnoTextPad.Features.Settings;

/// <summary>
/// The appearance of the editor: theme, font and word wrap. One instance is shared by every
/// open document so a change applies to all tabs at once, and every change is persisted.
/// </summary>
/// <remarks>
/// Loading is split in two so startup stays responsive: the stored preferences are cheap and
/// are applied before the window is shown, while enumerating the installed fonts is slower
/// and runs once the window is already visible.
/// </remarks>
public sealed partial class EditorSettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ISystemFontProvider _systemFontProvider;
    private readonly IThemeService _themeService;

    private EditorSettings _settings = new();

    /// <summary>Suppresses persistence while stored settings are being applied.</summary>
    private bool _isRestoring;

    public EditorSettingsViewModel(
        ISettingsRepository settingsRepository,
        ISystemFontProvider systemFontProvider,
        IThemeService themeService)
    {
        _settingsRepository = settingsRepository;
        _systemFontProvider = systemFontProvider;
        _themeService = themeService;
    }

    /// <summary>Every font family installed on this machine.</summary>
    public ObservableCollection<string> AvailableFontFamilies { get; } = [];

    /// <summary>The font sizes offered by the toolbar.</summary>
    public IReadOnlyList<double> AvailableFontSizes { get; } =
        [8, 9, 10, 11, 12, 13, 14, 16, 18, 20, 22, 24, 28, 32, 36, 48, 72];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorFontFamily))]
    public partial string SelectedFontFamilyName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double FontSize { get; set; } = EditorSettings.DefaultFontSize;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorTextWrapping))]
    public partial bool IsWordWrapEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThemeName))]
    public partial bool IsDarkTheme { get; set; }

    /// <summary>The name of the theme currently in use, shown on the toolbar toggle.</summary>
    public string ThemeName => IsDarkTheme ? "Dark mode" : "Light mode";

    /// <summary>The font the editor binds to, rebuilt whenever the selected family changes.</summary>
    public FontFamily EditorFontFamily => new(
        string.IsNullOrWhiteSpace(SelectedFontFamilyName) ? "Courier New" : SelectedFontFamilyName);

    public TextWrapping EditorTextWrapping => IsWordWrapEnabled ? TextWrapping.Wrap : TextWrapping.NoWrap;

    /// <summary>
    /// Reads the stored preferences and applies the theme. Call this before the window is
    /// shown so the app never appears in the wrong theme first.
    /// </summary>
    public async Task LoadPreferencesAsync(CancellationToken cancellationToken = default)
    {
        _settings = await _settingsRepository.LoadAsync(cancellationToken).ConfigureAwait(true);

        _isRestoring = true;

        try
        {
            FontSize = Math.Clamp(
                _settings.FontSize,
                EditorSettings.MinimumFontSize,
                EditorSettings.MaximumFontSize);
            IsWordWrapEnabled = _settings.WordWrap;

            // On first run there is no stored font, which is also how we know the theme has
            // never been chosen and should follow the operating system.
            IsDarkTheme = string.IsNullOrEmpty(_settings.FontFamily)
                ? _themeService.GetSystemTheme() == AppTheme.Dark
                : _settings.Theme == AppTheme.Dark;
        }
        finally
        {
            _isRestoring = false;
        }

        ApplyTheme();
    }

    /// <summary>
    /// Enumerates the installed fonts and selects the stored one, or a sensible default for
    /// this operating system. Call this once the window is visible.
    /// </summary>
    public async Task LoadFontsAsync(CancellationToken cancellationToken = default)
    {
        var installedFontFamilies = await _systemFontProvider
            .GetFontFamiliesAsync(cancellationToken).ConfigureAwait(true);

        foreach (var fontFamilyName in installedFontFamilies)
        {
            AvailableFontFamilies.Add(fontFamilyName);
        }

        SelectedFontFamilyName = ResolveFontFamily(_settings.FontFamily, installedFontFamilies);
    }

    public void ToggleTheme() => IsDarkTheme = !IsDarkTheme;

    partial void OnSelectedFontFamilyNameChanged(string value) => PersistIfLoaded();

    /// <summary>
    /// The font size box hands back <see cref="double.NaN"/> while it is empty or holds an
    /// unparseable value, so the last valid size is kept instead.
    /// </summary>
    partial void OnFontSizeChanged(double oldValue, double newValue)
    {
        if (double.IsNaN(newValue) || newValue <= 0)
        {
            FontSize = oldValue;
            return;
        }

        var clampedFontSize = Math.Clamp(
            newValue,
            EditorSettings.MinimumFontSize,
            EditorSettings.MaximumFontSize);

        if (!clampedFontSize.Equals(newValue))
        {
            FontSize = clampedFontSize;
            return;
        }

        PersistIfLoaded();
    }

    partial void OnIsWordWrapEnabledChanged(bool value) => PersistIfLoaded();

    partial void OnIsDarkThemeChanged(bool value)
    {
        if (_isRestoring)
        {
            return;
        }

        ApplyTheme();
        PersistIfLoaded();
    }

    private void ApplyTheme() => _themeService.Apply(IsDarkTheme ? AppTheme.Dark : AppTheme.Light);

    private void PersistIfLoaded()
    {
        if (_isRestoring || string.IsNullOrWhiteSpace(SelectedFontFamilyName))
        {
            return;
        }

        _settings.Theme = IsDarkTheme ? AppTheme.Dark : AppTheme.Light;
        _settings.FontFamily = SelectedFontFamilyName;
        _settings.FontSize = FontSize;
        _settings.WordWrap = IsWordWrapEnabled;

        _ = _settingsRepository.SaveAsync(_settings, CancellationToken.None);
    }

    private string ResolveFontFamily(string storedFontFamily, IReadOnlyList<string> installedFontFamilies)
    {
        var isStoredFontInstalled = !string.IsNullOrWhiteSpace(storedFontFamily)
            && installedFontFamilies.Contains(storedFontFamily, StringComparer.OrdinalIgnoreCase);

        return isStoredFontInstalled
            ? storedFontFamily
            : _systemFontProvider.ResolveDefaultFontFamily(installedFontFamilies);
    }
}
