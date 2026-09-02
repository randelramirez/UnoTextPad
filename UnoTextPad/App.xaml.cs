using System;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Resizetizer;
using UnoTextPad.Features.Editor;
using UnoTextPad.Infrastructure.DependencyInjection;
using UnoTextPad.Infrastructure.Storage;
using UnoTextPad.Infrastructure.Windowing;

namespace UnoTextPad;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private MainViewModel? _viewModel;

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }

    /// <remarks>
    /// The editor is a single page, so it is hosted directly in the window instead of behind
    /// a <see cref="Frame"/>. That removes a layer of navigation the app never uses and lets
    /// the page receive its view model through the constructor.
    /// </remarks>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _serviceProvider = new ServiceCollection().AddUnoTextPad().BuildServiceProvider();
        _serviceProvider.GetRequiredService<IAppDataPathProvider>().EnsureDirectoriesExist();

        MainWindow = new Window { Title = "UnoTextPad" };
#if DEBUG
        MainWindow.UseStudio();
#endif
        _serviceProvider.GetRequiredService<MainWindowProvider>().Window = MainWindow;

        var mainPage = _serviceProvider.GetRequiredService<MainPage>();
        _viewModel = mainPage.ViewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        MainWindow.Content = mainPage;
        MainWindow.SetWindowIcon();
        MainWindow.Closed += OnMainWindowClosed;

        // Preferences are a single small file, so applying them first costs almost nothing and
        // guarantees the window never appears in the wrong theme.
        await _viewModel.LoadPreferencesAsync();

        MainWindow.Activate();

        // Enumerating fonts and reopening the previous tabs happens with the window already
        // on screen, so startup stays immediate.
        await _viewModel.InitializeAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MainViewModel.WindowTitle) && MainWindow is not null && _viewModel is not null)
        {
            MainWindow.Title = _viewModel.WindowTitle;
        }
    }

    /// <summary>
    /// Writes the session one last time. The save path never resumes on the UI thread, so
    /// waiting for it here cannot deadlock, and the most recent edits are never lost.
    /// </summary>
    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        try
        {
            _viewModel?.SaveSessionNowAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            typeof(App).Log().LogError(exception, "Failed to save the session while closing.");
        }
        finally
        {
            _serviceProvider?.Dispose();
        }
    }

    /// <summary>
    /// Configures global Uno Platform logging
    /// </summary>
    public static void InitializeLogging()
    {
#if DEBUG
        // Logging is disabled by default for release builds, as it incurs a significant
        // initialization cost from Microsoft.Extensions.Logging setup. If startup performance
        // is a concern for your application, keep this disabled. If you're running on the web or
        // desktop targets, you can use URL or command line parameters to enable it.
        //
        // For more performance documentation: https://platform.uno/docs/articles/Uno-UI-Performance.html

        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());

            // Log to the Visual Studio Debug console
            builder.AddConsole();
#else
            builder.AddConsole();
#endif

            // Exclude logs below this level
            builder.SetMinimumLevel(LogLevel.Information);

            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);

            // Generic Xaml events
            // builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

            // Layouter specific messages
            // builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

            // builder.AddFilter("Windows.Storage", LogLevel.Debug );

            // Binding related messages
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

            // Binder memory references tracking
            // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

            // DevServer and HotReload related
            // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

            // Debug JS interop
            // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }
}
