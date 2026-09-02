using System.Windows.Input;
using Microsoft.UI.Xaml.Input;
using UnoTextPad.Features.Documents;
using Windows.System;

namespace UnoTextPad.Features.Editor;

/// <summary>
/// The editor window. The page keeps only view concerns: keyboard shortcuts, the caret
/// readout and restoring the caret of a tab; everything else lives in the view model.
/// </summary>
public sealed partial class MainPage : Page
{
    public MainPage(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        RegisterKeyboardAccelerators();
    }

    public MainViewModel ViewModel { get; }

    private void OnAddTabButtonClick(TabView sender, object args) => ViewModel.NewDocumentCommand.Execute(null);

    private async void OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is DocumentViewModel document)
        {
            await ViewModel.CloseAsync(document);
        }
    }

    /// <summary>Puts the caret back where it was when the tab was last active.</summary>
    private void OnEditorLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is not TextBox editor || editor.DataContext is not DocumentViewModel document)
        {
            return;
        }

        editor.Select(Math.Clamp(document.CaretPosition, 0, editor.Text.Length), 0);
        editor.Focus(FocusState.Programmatic);

    }

    private void OnEditorSelectionChanged(object sender, RoutedEventArgs args)
    {
        if (sender is TextBox editor)
        {
            ViewModel.UpdateCaretPosition(editor.SelectionStart);
        }
    }

    private void RegisterKeyboardAccelerators()
    {
        AddAccelerator(VirtualKey.N, ViewModel.NewDocumentCommand);
        AddAccelerator(VirtualKey.O, ViewModel.OpenFilesCommand);
        AddAccelerator(VirtualKey.S, ViewModel.SaveCommand);
        AddAccelerator(VirtualKey.S, ViewModel.SaveAsCommand, VirtualKeyModifiers.Shift);
        AddAccelerator(VirtualKey.W, ViewModel.CloseDocumentCommand);
    }

    /// <summary>
    /// Registers a shortcut under both Control and Windows modifiers, because macOS reports
    /// its Command key as the Windows modifier while Windows and Linux use Control.
    /// </summary>
    private void AddAccelerator(
        VirtualKey key,
        ICommand command,
        VirtualKeyModifiers additionalModifiers = VirtualKeyModifiers.None)
    {
        foreach (var commandModifier in new[] { VirtualKeyModifiers.Control, VirtualKeyModifiers.Windows })
        {
            var accelerator = new KeyboardAccelerator
            {
                Key = key,
                Modifiers = commandModifier | additionalModifiers
            };

            accelerator.Invoked += (sender, invokedArgs) =>
            {
                invokedArgs.Handled = true;

                if (command.CanExecute(null))
                {
                    command.Execute(null);
                }
            };

            KeyboardAccelerators.Add(accelerator);
        }
    }
}
