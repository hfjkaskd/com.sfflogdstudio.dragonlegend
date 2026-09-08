using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredSlotSettlementTests
{
    private static RecoveredSlotSettlement Create()
    {
        return new RecoveredSlotSettlement(new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
            Gimrol = new GimrolPoro {
                J3 = new List<int>{30,30,30,30,30,30,30,3},
                J4 = new List<int>{60,60,60,60,60,60,60,6},
                J5 = new List<int>{90,90,90,90,90,90,90,9},
                Lingg = new List<int>{30}
            }
        }));
    }
    private static int[,] Empty()
    {
        var board = new int[5,3];
        for (int c = 0; c < 5; c++) for (int r = 0; r < 3; r++) board[c,r] = 10;
        return board;
    }

    [Test]
    public void CrossRowBranchesRemainDistinctAndPreservePositions()
    {
        var result = Create(); var board = Empty();
        board[0,2] = 1; board[1,0] = 1; board[1,2] = 1;
        board[2,1] = 1; board[3,0] = 2;
        result.Evaluate(board, 2);
        Assert.AreEqual(4f, result.RawAward);
        Assert.AreEqual(2, result.WinningLines.Count);
        Assert.AreEqual(2, result.WinningLines[0].GetRow(0));
        Assert.AreEqual(0, result.WinningLines[0].GetRow(1));
        Assert.AreEqual(2, result.WinningLines[1].GetRow(1));
        Assert.AreEqual(1, result.WinningLines[1].GetRow(2));
    }

    [Test]
    public void ExtendedPrefixIsNotAlsoPaidAsThreeOfAKind()
    {
        var result = Create(); var board = Empty();
        board[0,0] = board[1,0] = board[2,0] = board[3,1] = 1;
        board[3,0] = 2; board[4,0] = 2;
        result.Evaluate(board, 1);
        Assert.AreEqual(2f, result.RawAward);
        Assert.AreEqual(1, result.WinningLines.Count);
        Assert.AreEqual(4, result.WinningLines[0].Count);
    }

    [Test]
    public void NativeSpecialOnlyColumnDoesNotCollectTerminatedPrefix()
    {
        var result = Create(); var board = Empty();
        board[0,0] = board[1,0] = board[2,0] = 1;
        result.Evaluate(board, 1);
        Assert.AreEqual(0f, result.RawAward);
        Assert.IsEmpty(result.WinningLines);
    }

    [Test]
    public void ThreeLeadingWildsReplaceHigherRegularPay()
    {
        var result = Create(); var board = Empty();
        board[0,0] = board[1,0] = board[2,0] = 7;
        board[3,0] = board[4,0] = 1;
        result.Evaluate(board, 1);
        Assert.AreEqual(0.1f, result.RawAward);
        Assert.AreEqual(1f, result.GetWinTotalLine());
        Assert.AreEqual(1f, result.RawAward);
    }

    [Test]
    public void FullWildBoardProduces243PathsAndBufferResets()
    {
        var result = Create(); var board = new int[5,3];
        for (int c = 0; c < 5; c++) for (int r = 0; r < 3; r++) board[c,r] = 7;
        result.Evaluate(board, 10);
        Assert.AreEqual(243, result.WinningLines.Count);
        Assert.AreEqual(729f, result.RawAward);
        result.Evaluate(Empty(), 10);
        Assert.AreEqual(0, result.WinningLines.Count);
        Assert.AreEqual(0f, result.GetWinTotalLine());
        result.Evaluate(board, 10);
        Assert.AreEqual(243, result.WinningLines.Count);
    }

    [Test]
    public void BetMultiplicationPreservesNativeSignedIntegerOverflow()
    {
        var result = Create(); var board = Empty();
        for (int c = 0; c < 5; c++) board[c,0] = 1;
        result.Evaluate(board, int.MaxValue);
        Assert.AreEqual(-3f, result.RawAward);
    }
}
