namespace UnoTextPad.Tests.Features.Documents;

public class CaretLocationTests
{
    [Fact]
    public void Calculate_AtStartOfEmptyText_ReturnsFirstLineAndColumn()
        => Assert.Equal(new CaretLocation(1, 1), CaretLocation.Calculate(string.Empty, 0));

    [Fact]
    public void Calculate_OnFirstLine_CountsColumnsFromOne()
        => Assert.Equal(new CaretLocation(1, 6), CaretLocation.Calculate("hello world", 5));

    [Fact]
    public void Calculate_ImmediatelyAfterLineFeed_StartsANewLineAtColumnOne()
        => Assert.Equal(new CaretLocation(2, 1), CaretLocation.Calculate("first\nsecond", 6));

    [Fact]
    public void Calculate_OnThirdLine_ReportsBothLineAndColumn()
        => Assert.Equal(new CaretLocation(3, 3), CaretLocation.Calculate("one\ntwo\nthree", 10));

    [Fact]
    public void Calculate_WithWindowsLineEndings_CountsEachLineOnce()
        => Assert.Equal(new CaretLocation(3, 1), CaretLocation.Calculate("one\r\ntwo\r\nthree", 10));

    [Fact]
    public void Calculate_WithCarriageReturnOnlyText_StillCountsLines()
        => Assert.Equal(new CaretLocation(3, 1), CaretLocation.Calculate("one\rtwo\rthree", 8));

    [Fact]
    public void Calculate_BeyondEndOfText_ClampsToTheLastPosition()
        => Assert.Equal(new CaretLocation(1, 4), CaretLocation.Calculate("abc", 99));

    [Fact]
    public void Calculate_WithNegativeIndex_ReturnsFirstLineAndColumn()
        => Assert.Equal(new CaretLocation(1, 1), CaretLocation.Calculate("abc", -5));
}
