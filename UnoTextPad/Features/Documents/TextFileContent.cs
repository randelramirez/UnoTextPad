namespace UnoTextPad.Features.Documents;

/// <summary>
/// The text of a file together with the encoding and line ending style it was stored with.
/// </summary>
/// <param name="Text">The decoded text, normalized so that every line ends with a single "\n".</param>
/// <param name="Encoding">The encoding the file was read with, and should be written back with.</param>
/// <param name="LineEnding">The line ending style the file used, and should be written back with.</param>
public sealed record TextFileContent(string Text, TextEncodingKind Encoding, LineEndingStyle LineEnding);
