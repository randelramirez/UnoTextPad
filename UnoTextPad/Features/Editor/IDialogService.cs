namespace UnoTextPad.Features.Editor;

/// <summary>Shows the modal prompts the editor needs.</summary>
public interface IDialogService
{
    Task<SaveChangesChoice> AskToSaveChangesAsync(string documentName);

    Task ShowMessageAsync(string title, string message);
}
