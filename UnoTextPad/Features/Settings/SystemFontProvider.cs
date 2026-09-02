using SkiaSharp;

namespace UnoTextPad.Features.Settings;

/// <summary>
/// Enumerates fonts through SkiaSharp, which is the same font manager Uno Platform's Skia
/// renderer resolves <c>FontFamily</c> names against, so every name listed here is renderable.
/// </summary>
public sealed class SystemFontProvider : ISystemFontProvider
{
    private static readonly string[] PreferredWindowsFonts = ["Consolas", "Cascadia Mono", "Courier New"];
    private static readonly string[] PreferredMacFonts = ["Menlo", "SF Mono", "Monaco", "Courier New"];
    private static readonly string[] PreferredLinuxFonts =
        ["DejaVu Sans Mono", "Ubuntu Mono", "Liberation Mono", "Noto Sans Mono"];

    private IReadOnlyList<string>? _cachedFontFamilies;

    public async Task<IReadOnlyList<string>> GetFontFamiliesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedFontFamilies is not null)
        {
            return _cachedFontFamilies;
        }

        // Enumerating the system font manager touches the disk, so keep it off the UI thread.
        _cachedFontFamilies = await Task.Run(
            () => (IReadOnlyList<string>)SKFontManager.Default
                .GetFontFamilies()
                .Where(fontFamilyName => !string.IsNullOrWhiteSpace(fontFamilyName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(fontFamilyName => fontFamilyName, StringComparer.CurrentCultureIgnoreCase)
                .ToArray(),
            cancellationToken).ConfigureAwait(false);

        return _cachedFontFamilies;
    }

    public string ResolveDefaultFontFamily(IReadOnlyList<string> installedFontFamilies)
    {
        foreach (var preferredFontFamily in GetPreferredFontFamilies())
        {
            var match = installedFontFamilies.FirstOrDefault(
                fontFamilyName => string.Equals(fontFamilyName, preferredFontFamily, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match;
            }
        }

        var anyMonospacedFont = installedFontFamilies.FirstOrDefault(
            fontFamilyName => fontFamilyName.Contains("Mono", StringComparison.OrdinalIgnoreCase));

        return anyMonospacedFont ?? installedFontFamilies.FirstOrDefault() ?? "Courier New";
    }

    private static string[] GetPreferredFontFamilies()
    {
        if (OperatingSystem.IsWindows())
        {
            return PreferredWindowsFonts;
        }

        return OperatingSystem.IsMacOS() ? PreferredMacFonts : PreferredLinuxFonts;
    }
}
