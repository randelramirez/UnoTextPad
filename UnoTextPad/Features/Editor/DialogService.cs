using UnoTextPad.Infrastructure.Windowing;

namespace UnoTextPad.Features.Editor;

/// <inheritdoc cref="IDialogService"/>
public sealed class DialogService : IDialogService
{
    private readonly IMainWindowProvider _windowProvider;

    public DialogService(IMainWindowProvider windowProvider) => _windowProvider = windowProvider;

    public async Task<SaveChangesChoice> AskToSaveChangesAsync(string documentName)
    {
        if (_windowProvider.XamlRoot is not { } xamlRoot)
        {
            return SaveChangesChoice.Cancel;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            RequestedTheme = GetRootTheme(),
            Title = "Save changes?",
            Content = $"\"{documentName}\" has unsaved changes.",
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Don't save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();

        return result switch
        {
            ContentDialogResult.Primary => SaveChangesChoice.Save,
            ContentDialogResult.Secondary => SaveChangesChoice.Discard,
            _ => SaveChangesChoice.Cancel
        };
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        if (_windowProvider.XamlRoot is not { } xamlRoot)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            RequestedTheme = GetRootTheme(),
            Title = title,
            Content = message,
            CloseButtonText = "OK"
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Dialogs live in a popup rather than inside the page, so they have to be told which
    /// theme the page is currently using.
    /// </summary>
    private ElementTheme GetRootTheme()
        => _windowProvider.ThemeRoot?.RequestedTheme ?? ElementTheme.Default;
}
