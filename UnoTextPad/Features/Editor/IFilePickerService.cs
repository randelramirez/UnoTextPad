namespace UnoTextPad.Features.Editor;

/// <summary>
/// Shows the operating system's open and save dialogs. Returns plain file paths so that
/// view models never depend on the storage APIs.
/// </summary>
public interface IFilePickerService
{
    /// <summary>Returns the chosen file paths, or an empty list when the user cancels.</summary>
    Task<IReadOnlyList<string>> PickFilesToOpenAsync();

    /// <summary>Returns the chosen file path, or <c>null</c> when the user cancels.</summary>
    Task<string?> PickFileToSaveAsync(string suggestedFileName);
}
