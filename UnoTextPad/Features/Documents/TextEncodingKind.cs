namespace UnoTextPad.Features.Documents;

/// <summary>
/// The text encodings the editor can read and write. Detected from the byte order
/// mark when a file is opened so that saving preserves the original encoding.
/// </summary>
public enum TextEncodingKind
{
    /// <summary>UTF-8 without a byte order mark.</summary>
    Utf8,

    /// <summary>UTF-8 with a byte order mark.</summary>
    Utf8Bom,

    /// <summary>UTF-16 little endian with a byte order mark.</summary>
    Utf16Le,

    /// <summary>UTF-16 big endian with a byte order mark.</summary>
    Utf16Be
}
