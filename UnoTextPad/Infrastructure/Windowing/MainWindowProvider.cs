namespace UnoTextPad.Infrastructure.Windowing;

/// <inheritdoc cref="IMainWindowProvider"/>
public sealed class MainWindowProvider : IMainWindowProvider
{
    public Window? Window { get; set; }

    public XamlRoot? XamlRoot => (Window?.Content as FrameworkElement)?.XamlRoot;

    public FrameworkElement? ThemeRoot => Window?.Content as FrameworkElement;
}
