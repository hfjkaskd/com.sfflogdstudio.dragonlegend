using DragonLegend.Whitebox;
using NUnit.Framework;

public sealed class RecoveredSlotResultTests
{
    [Test]
    public void ScatterUsesSecondOccurrenceColumnAndNativeZeroSentinel()
    {
        var board = new int[5,3];
        Assert.AreEqual(-1, RecoveredSlotResultRules.SpeedRoll(board, 0));
        board[0,0] = 10; board[2,1] = 10;
        Assert.AreEqual(2, RecoveredSlotResultRules.SpeedRoll(board, 2));
        Assert.AreEqual(-1, RecoveredSlotResultRules.SpeedRoll(board, 1));
        board[2,1] = 0; board[0,1] = 10;
        Assert.AreEqual(-1, RecoveredSlotResultRules.SpeedRoll(board, 2));
    }

    [Test]
    public void WildRequiresTwoFullColumnsAndChoosesEarlierTrigger()
    {
        var board = new int[5,3];
        board[0,0] = 10; board[4,0] = 10;
        for (int row = 0; row < 3; row++) board[1,row] = 7;
        board[3,0] = 7; board[3,1] = 7;
        Assert.AreEqual(4, RecoveredSlotResultRules.SpeedRoll(board, 2));
        board[3,2] = 7;
        Assert.AreEqual(3, RecoveredSlotResultRules.SpeedRoll(board, 2));
        board[2,0] = 10;
        Assert.AreEqual(2, RecoveredSlotResultRules.SpeedRoll(board, 3));
        Assert.AreEqual(3, RecoveredSlotResultRules.SpeedRoll(board, 0));
    }

    [TestCase(new int[] {7,7,7,0,7}, true, 3)]
    [TestCase(new int[] {7,7,0,7,7}, false, 0)]
    [TestCase(new int[] {0,7,7,7,7}, false, 0)]
    [TestCase(new int[] {7,7,7,7,7}, true, 5)]
    [TestCase(new int[] {}, false, 0)]
    public void WildAwardUsesConsecutivePrefix(int[] symbols, bool expected, int expectedCount)
    {
        Assert.AreEqual(expected, RecoveredSlotResultRules.HasWildEqualMoreThanThree(symbols, out int count));
        Assert.AreEqual(expectedCount, count);
    }

    [TestCase(0f, 0f)]
    [TestCase(0.01f, 1f)]
    [TestCase(1.5f, 1.5f)]
    [TestCase(-1f, -1f)]
    public void PositiveFractionAwardIsWrittenBack(float value, float expected)
    {
        Assert.AreEqual(expected, RecoveredSlotResultRules.GetWinTotalLine(ref value));
        Assert.AreEqual(expected, value);
    }
}
