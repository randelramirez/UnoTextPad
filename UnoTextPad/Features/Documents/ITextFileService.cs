namespace UnoTextPad.Features.Documents;

/// <summary>
/// Reads and writes the plain text files the user edits, preserving each file's original
/// encoding and line ending style.
/// </summary>
public interface ITextFileService
{
    Task<TextFileContent> ReadAsync(string filePath, CancellationToken cancellationToken = default);

    Task WriteAsync(string filePath, TextFileContent content, CancellationToken cancellationToken = default);
}
