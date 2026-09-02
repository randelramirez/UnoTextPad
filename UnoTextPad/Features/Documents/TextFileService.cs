using System.Text;

namespace UnoTextPad.Features.Documents;

/// <inheritdoc cref="ITextFileService"/>
public sealed class TextFileService : ITextFileService
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);
    private static readonly UnicodeEncoding Utf16LittleEndian = new(bigEndian: false, byteOrderMark: true);
    private static readonly UnicodeEncoding Utf16BigEndian = new(bigEndian: true, byteOrderMark: true);

    public async Task<TextFileContent> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        var encoding = DetectEncoding(bytes);
        var text = Decode(bytes, encoding);
        var lineEnding = DetectLineEnding(text);

        return new TextFileContent(NormalizeToLineFeed(text), encoding, lineEnding);
    }

    public Task WriteAsync(
        string filePath,
        TextFileContent content,
        CancellationToken cancellationToken = default)
    {
        var text = ApplyLineEnding(content.Text, content.LineEnding);
        return File.WriteAllTextAsync(filePath, text, ToEncoding(content.Encoding), cancellationToken);
    }

    /// <summary>
    /// Replaces every line ending with a single "\n" so that the editor and the caret
    /// position arithmetic only ever deal with one representation.
    /// </summary>
    public static string NormalizeToLineFeed(string text)
        => text.Contains('\r') ? text.Replace("\r\n", "\n").Replace('\r', '\n') : text;

    /// <summary>Rewrites the normalized text using the style the file was stored with.</summary>
    public static string ApplyLineEnding(string text, LineEndingStyle lineEnding)
    {
        var normalized = NormalizeToLineFeed(text);

        return lineEnding switch
        {
            LineEndingStyle.CrLf => normalized.Replace("\n", "\r\n"),
            LineEndingStyle.Cr => normalized.Replace('\n', '\r'),
            _ => normalized
        };
    }

    public static TextEncodingKind DetectEncoding(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return TextEncodingKind.Utf8Bom;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return TextEncodingKind.Utf16Le;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return TextEncodingKind.Utf16Be;
        }

        return TextEncodingKind.Utf8;
    }

    public static LineEndingStyle DetectLineEnding(string text)
    {
        var firstBreakIndex = text.AsSpan().IndexOfAny('\r', '\n');

        if (firstBreakIndex < 0)
        {
            return OperatingSystem.IsWindows() ? LineEndingStyle.CrLf : LineEndingStyle.Lf;
        }

        if (text[firstBreakIndex] == '\n')
        {
            return LineEndingStyle.Lf;
        }

        var isFollowedByLineFeed = firstBreakIndex + 1 < text.Length && text[firstBreakIndex + 1] == '\n';
        return isFollowedByLineFeed ? LineEndingStyle.CrLf : LineEndingStyle.Cr;
    }

    private static string Decode(byte[] bytes, TextEncodingKind encoding) => encoding switch
    {
        TextEncodingKind.Utf8Bom => Utf8WithoutBom.GetString(bytes, 3, bytes.Length - 3),
        TextEncodingKind.Utf16Le => Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2),
        TextEncodingKind.Utf16Be => Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2),
        _ => Utf8WithoutBom.GetString(bytes)
    };

    private static Encoding ToEncoding(TextEncodingKind encoding) => encoding switch
    {
        TextEncodingKind.Utf8Bom => Utf8WithBom,
        TextEncodingKind.Utf16Le => Utf16LittleEndian,
        TextEncodingKind.Utf16Be => Utf16BigEndian,
        _ => Utf8WithoutBom
    };
}
