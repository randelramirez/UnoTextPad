using UnoTextPad.Infrastructure.Windowing;

namespace UnoTextPad.Features.Settings;

/// <summary>
/// Switches themes by setting <c>RequestedTheme</c> on the root element. Element level
/// theming is fully supported by Uno Platform's Skia renderer, so the change is immediate
/// and does not require restarting the app.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private readonly IMainWindowProvider _windowProvider;

    public ThemeService(IMainWindowProvider windowProvider) => _windowProvider = windowProvider;

    public void Apply(AppTheme theme)
    {
        if (_windowProvider.ThemeRoot is { } themeRoot)
        {
            themeRoot.RequestedTheme = theme == AppTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

    public AppTheme GetSystemTheme()
        => Application.Current.RequestedTheme == ApplicationTheme.Dark ? AppTheme.Dark : AppTheme.Light;
}
