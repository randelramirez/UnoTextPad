namespace UnoTextPad.Features.Settings;

/// <summary>
/// Lists the font families installed on the machine so the user can choose one for the editor.
/// </summary>
public interface ISystemFontProvider
{
    /// <summary>Returns the installed font family names, sorted and without duplicates.</summary>
    Task<IReadOnlyList<string>> GetFontFamiliesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Picks the font to use on first run: a fixed-width family this operating system is
    /// known to ship, falling back to any installed monospaced family.
    /// </summary>
    string ResolveDefaultFontFamily(IReadOnlyList<string> installedFontFamilies);
}
