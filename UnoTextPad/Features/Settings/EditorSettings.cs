namespace UnoTextPad.Features.Settings;

/// <summary>
/// The user preferences that survive a restart. Persisted as JSON next to the session.
/// </summary>
public sealed class EditorSettings
{
    /// <summary>The smallest font size the editor offers.</summary>
    public const double MinimumFontSize = 8d;

    /// <summary>The largest font size the editor offers.</summary>
    public const double MaximumFontSize = 72d;

    /// <summary>The font size used when no preference has been stored yet.</summary>
    public const double DefaultFontSize = 14d;

    public AppTheme Theme { get; set; } = AppTheme.Light;

    /// <summary>
    /// The system font family name used by the editor. Empty means "pick a sensible
    /// default for this machine the next time the app starts".
    /// </summary>
    public string FontFamily { get; set; } = string.Empty;

    public double FontSize { get; set; } = DefaultFontSize;

    public bool WordWrap { get; set; }
}
