namespace UnoTextPad.Features.Documents;

/// <summary>
/// The line terminator a text file uses. Detected when a file is opened so that
/// saving the file does not silently rewrite every line ending.
/// </summary>
public enum LineEndingStyle
{
    /// <summary>Unix / macOS style "\n".</summary>
    Lf,

    /// <summary>Windows style "\r\n".</summary>
    CrLf,

    /// <summary>Classic Mac style "\r".</summary>
    Cr
}
