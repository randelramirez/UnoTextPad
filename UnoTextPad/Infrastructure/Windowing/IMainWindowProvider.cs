namespace UnoTextPad.Infrastructure.Windowing;

/// <summary>
/// Gives services that need the application window — file pickers, dialogs and theming —
/// access to it without depending on the <c>App</c> class itself.
/// </summary>
public interface IMainWindowProvider
{
    Window? Window { get; }

    /// <summary>The element dialogs are shown in, or <c>null</c> before the window has content.</summary>
    XamlRoot? XamlRoot { get; }

    /// <summary>The element whose <c>RequestedTheme</c> controls the theme of the whole app.</summary>
    FrameworkElement? ThemeRoot { get; }
}
