using System.Text;

namespace UnoTextPad.Tests.Features.Documents;

public class TextFileServiceTests : IDisposable
{
    private readonly string _temporaryFolder;

    public TextFileServiceTests()
    {
        _temporaryFolder = Path.Combine(Path.GetTempPath(), $"unotextpad-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_temporaryFolder);
    }

    public void Dispose() => Directory.Delete(_temporaryFolder, recursive: true);

    [Fact]
    public void DetectEncoding_WithoutByteOrderMark_ReturnsUtf8()
        => Assert.Equal(TextEncodingKind.Utf8, TextFileService.DetectEncoding("plain"u8));

    [Fact]
    public void DetectEncoding_WithUtf8ByteOrderMark_ReturnsUtf8Bom()
        => Assert.Equal(TextEncodingKind.Utf8Bom, TextFileService.DetectEncoding([0xEF, 0xBB, 0xBF, 0x41]));

    [Fact]
    public void DetectEncoding_WithLittleEndianMark_ReturnsUtf16Le()
        => Assert.Equal(TextEncodingKind.Utf16Le, TextFileService.DetectEncoding([0xFF, 0xFE, 0x41, 0x00]));

    [Fact]
    public void DetectEncoding_WithBigEndianMark_ReturnsUtf16Be()
        => Assert.Equal(TextEncodingKind.Utf16Be, TextFileService.DetectEncoding([0xFE, 0xFF, 0x00, 0x41]));

    [Fact]
    public void DetectLineEnding_WithWindowsEndings_ReturnsCrLf()
        => Assert.Equal(LineEndingStyle.CrLf, TextFileService.DetectLineEnding("one\r\ntwo"));

    [Fact]
    public void DetectLineEnding_WithUnixEndings_ReturnsLf()
        => Assert.Equal(LineEndingStyle.Lf, TextFileService.DetectLineEnding("one\ntwo"));

    [Fact]
    public void DetectLineEnding_WithClassicMacEndings_ReturnsCr()
        => Assert.Equal(LineEndingStyle.Cr, TextFileService.DetectLineEnding("one\rtwo"));

    [Fact]
    public void NormalizeToLineFeed_CollapsesEveryStyleToLineFeed()
        => Assert.Equal("a\nb\nc\nd", TextFileService.NormalizeToLineFeed("a\r\nb\rc\nd"));

    [Fact]
    public void ApplyLineEnding_RestoresTheOriginalStyle()
        => Assert.Equal("a\r\nb", TextFileService.ApplyLineEnding("a\nb", LineEndingStyle.CrLf));

    [Fact]
    public async Task ReadAsync_NormalizesWindowsLineEndingsButRemembersThem()
    {
        var filePath = await WriteFileAsync("windows.txt", "one\r\ntwo", new UTF8Encoding(false));

        var content = await new TextFileService().ReadAsync(filePath, Token);

        Assert.Equal("one\ntwo", content.Text);
        Assert.Equal(LineEndingStyle.CrLf, content.LineEnding);
        Assert.Equal(TextEncodingKind.Utf8, content.Encoding);
    }

    [Fact]
    public async Task WriteAsync_RoundTripsTextEncodingAndLineEndings()
    {
        var service = new TextFileService();
        var filePath = Path.Combine(_temporaryFolder, "roundtrip.txt");
        var original = new TextFileContent("alpha\nbeta", TextEncodingKind.Utf8Bom, LineEndingStyle.CrLf);

        await service.WriteAsync(filePath, original, Token);
        var reloaded = await service.ReadAsync(filePath, Token);

        Assert.Equal(original, reloaded);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, (await File.ReadAllBytesAsync(filePath, Token))[..3]);
        Assert.Contains("alpha\r\nbeta", await File.ReadAllTextAsync(filePath, Token));
    }

    [Fact]
    public async Task ReadAsync_PreservesUtf16Content()
    {
        var filePath = await WriteFileAsync("utf16.txt", "grüße\nwelt", new UnicodeEncoding(false, true));

        var content = await new TextFileService().ReadAsync(filePath, Token);

        Assert.Equal(TextEncodingKind.Utf16Le, content.Encoding);
        Assert.Equal("grüße\nwelt", content.Text);
    }

    private async Task<string> WriteFileAsync(string fileName, string text, Encoding encoding)
    {
        var filePath = Path.Combine(_temporaryFolder, fileName);
        await File.WriteAllTextAsync(filePath, text, encoding, Token);
        return filePath;
    }
}
