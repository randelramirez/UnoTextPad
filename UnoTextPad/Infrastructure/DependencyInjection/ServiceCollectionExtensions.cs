using Microsoft.Extensions.DependencyInjection;
using UnoTextPad.Features.Documents;
using UnoTextPad.Features.Editor;
using UnoTextPad.Features.Session;
using UnoTextPad.Features.Settings;
using UnoTextPad.Infrastructure.Storage;
using UnoTextPad.Infrastructure.Windowing;

namespace UnoTextPad.Infrastructure.DependencyInjection;

/// <summary>
/// The composition root. Every dependency is registered against its interface so that
/// implementations can be replaced without touching the view models.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUnoTextPad(this IServiceCollection services)
    {
        // The window is created after the container, so the provider is registered as a
        // concrete singleton that App fills in and everything else consumes as an interface.
        services.AddSingleton<MainWindowProvider>();
        services.AddSingleton<IMainWindowProvider>(
            serviceProvider => serviceProvider.GetRequiredService<MainWindowProvider>());

        services.AddSingleton<IAppDataPathProvider, AppDataPathProvider>();
        services.AddSingleton<IJsonFileStore, JsonFileStore>();
        services.AddSingleton<ITextFileService, TextFileService>();
        services.AddSingleton<ISessionRepository, SessionRepository>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        services.AddSingleton<ISystemFontProvider, SystemFontProvider>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IDocumentSessionCoordinator, DocumentSessionCoordinator>();

        services.AddSingleton<EditorSettingsViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainPage>();

        return services;
    }
}
