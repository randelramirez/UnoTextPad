namespace UnoTextPad.Features.Settings;

/// <summary>Applies the light or dark theme to the running application.</summary>
public interface IThemeService
{
    void Apply(AppTheme theme);

    /// <summary>The theme the operating system is currently using, used on first run.</summary>
    AppTheme GetSystemTheme();
}
