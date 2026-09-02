using UnoTextPad.Infrastructure.Windowing;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace UnoTextPad.Features.Editor;

/// <summary>
/// Wraps the Windows.Storage pickers, which Uno Platform maps onto the native dialog of
/// each desktop platform (Win32, AppKit and the Linux file chooser).
/// </summary>
public sealed class FilePickerService : IFilePickerService
{
    private readonly IMainWindowProvider _windowProvider;

    public FilePickerService(IMainWindowProvider windowProvider) => _windowProvider = windowProvider;

    public async Task<IReadOnlyList<string>> PickFilesToOpenAsync()
    {
        var picker = new FileOpenPicker
        {
            // Both of these must be set or the picker throws on Windows, even though
            // the suggested location is ignored on the other desktop platforms.
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };

        // "*" lets the user open any file, which is what a general purpose text editor needs.
        picker.FileTypeFilter.Add("*");
        AssociateWithWindow(picker);

        var pickedFiles = await picker.PickMultipleFilesAsync();

        return pickedFiles is null
            ? []
            : pickedFiles.Select(pickedFile => pickedFile.Path).Where(Path.IsPathRooted).ToArray();
    }

    public async Task<string?> PickFileToSaveAsync(string suggestedFileName)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName
        };

        picker.FileTypeChoices.Add("Text Document", [".txt", ".md", ".log", ".csv", ".xml", ".ini"]);
        picker.FileTypeChoices.Add("JSON File", [".json"]);
        AssociateWithWindow(picker);

        StorageFile? savedFile = await picker.PickSaveFileAsync();

        return savedFile is null || !Path.IsPathRooted(savedFile.Path) ? null : savedFile.Path;
    }

    /// <summary>
    /// Associates a picker with the window that owns it. This is a no-op on the Skia desktop
    /// targets but is required if the app is ever also built for the Windows App SDK.
    /// </summary>
    private void AssociateWithWindow(object picker)
    {
        if (_windowProvider.Window is not { } window)
        {
            return;
        }

        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
    }
}
