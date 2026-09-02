namespace UnoTextPad.Features.Documents;

/// <summary>
/// A one-based line and column pair, as shown in the status bar.
/// </summary>
public readonly record struct CaretLocation(int Line, int Column)
{
    /// <summary>
    /// Converts a character offset into a line and column.
    /// </summary>
    /// <remarks>
    /// Counting is done with vectorized span searches rather than a character loop so that
    /// moving the caret stays cheap even in a multi-megabyte file. Text normalized to "\n"
    /// takes the first branch; text that only uses "\r" takes the second.
    /// </remarks>
    public static CaretLocation Calculate(string text, int caretIndex)
    {
        if (caretIndex <= 0 || text.Length == 0)
        {
            return new CaretLocation(1, 1);
        }

        var offset = Math.Min(caretIndex, text.Length);
        var precedingText = text.AsSpan(0, offset);

        var lineFeedCount = precedingText.Count('\n');

        if (lineFeedCount > 0)
        {
            return new CaretLocation(lineFeedCount + 1, offset - precedingText.LastIndexOf('\n'));
        }

        var carriageReturnCount = precedingText.Count('\r');

        return carriageReturnCount > 0
            ? new CaretLocation(carriageReturnCount + 1, offset - precedingText.LastIndexOf('\r'))
            : new CaretLocation(1, offset + 1);
    }
}
