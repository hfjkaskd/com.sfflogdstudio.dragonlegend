using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredSymbolWinSelectionTests
{
    private static RecoveredGameplayRules Rules(params int[] thresholds)
        =>new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
            Qonrii=new QonriiPoro{Riikin=new List<int>(thresholds)},
            Gimrol=new GimrolPoro{Lingg=new List<int>{30},J3=new List<int>{30,30,30,30,30,30,30,3},
                J4=new List<int>{60,60,60,60,60,60,60,6},J5=new List<int>{90,90,90,90,90,90,90,9}}
        });
    [TestCase(19.99f,2,RecoveredSlotWinType.None)]
    [TestCase(20,2,RecoveredSlotWinType.Big)]
    [TestCase(39.99f,2,RecoveredSlotWinType.Big)]
    [TestCase(40,2,RecoveredSlotWinType.Mega)]
    [TestCase(100,2,RecoveredSlotWinType.Super)]
    [TestCase(0,0,RecoveredSlotWinType.Super)]
    public void NativeBigWinBoundaries(float amount,int bet,RecoveredSlotWinType expected)
        =>Assert.AreEqual(expected,Rules(10,20,50).GetBigWin(amount,bet));
    [Test]
    public void ThresholdOrderExtraEntriesAndSignedOverflowRemainNative()
    {
        Assert.AreEqual(RecoveredSlotWinType.Super,Rules(50,20,10).GetBigWin(10,1));
        Assert.AreEqual(RecoveredSlotWinType.None,Rules(10,20,50,5).GetBigWin(100,1));
        Assert.AreEqual(RecoveredSlotWinType.Big,Rules(int.MaxValue).GetBigWin(-1,2));
        Assert.AreEqual(RecoveredSlotWinType.None,Rules().GetBigWin(100,2));
    }
    [Test]
    public void WinningPathsDeduplicateCellsInEncounterOrderAndUseLiveWildIdentity()
    {
        var rules=Rules(1,2,5);var settlement=new RecoveredSlotSettlement(rules);var board=new int[5,3];
        for(int c=0;c<5;c++)for(int r=0;r<3;r++)board[c,r]=10;
        board[0,2]=1;board[1,0]=7;board[1,2]=1;board[2,1]=1;board[3,0]=2;
        settlement.Evaluate(board,2);var selection=new RecoveredSymbolWinSelection(rules);
        selection.Capture(settlement,(c,r)=>board[c,r],3,2);
        Assert.AreEqual(4,selection.LineWin);Assert.AreEqual(7,selection.TotalWin);
        Assert.AreEqual(RecoveredSlotWinType.Mega,selection.BigWin);Assert.IsTrue(selection.RequestsLineSound);
        Assert.AreEqual(4,selection.Count);
        int[] columns={0,1,2,1},rows={2,0,1,2},ids={1,7,1,1};
        for(int i=0;i<4;i++){var cell=selection.At(i);Assert.AreEqual(columns[i],cell.Column);Assert.AreEqual(rows[i],cell.Row);Assert.AreEqual(ids[i],cell.SymbolId);}
    }
    [Test]
    public void MinimumLineAwardPrecedesBonusAndZeroTotalSkipsSelection()
    {
        var rules=Rules(1,2,5);var settlement=new RecoveredSlotSettlement(rules);var board=new int[5,3];
        for(int c=0;c<5;c++)for(int r=0;r<3;r++)board[c,r]=7;
        settlement.Evaluate(board,1);var selection=new RecoveredSymbolWinSelection(rules);
        selection.Capture(settlement,(c,r)=>board[c,r],.25f,1);
        Assert.AreEqual(72.9f,selection.LineWin);Assert.AreEqual(73.15f,selection.TotalWin,.0001f);Assert.AreEqual(15,selection.Count);
        selection.Capture(settlement,(c,r)=>{Assert.Fail("Zero total must not read cells");return 0;},-72.9f,1);
        Assert.IsFalse(selection.HasReward);Assert.IsFalse(selection.RequestsLineSound);Assert.AreEqual(15,selection.Count);
        Assert.AreEqual(RecoveredSlotWinType.None,selection.BigWin);
        for(int c=0;c<5;c++)for(int r=0;r<3;r++)board[c,r]=10;
        board[0,0]=board[1,0]=board[2,0]=7;board[3,0]=board[4,0]=1;
        settlement.Evaluate(board,1);Assert.AreEqual(.1f,settlement.RawAward);
        selection.Capture(settlement,(c,r)=>board[c,r],.25f,1);
        Assert.AreEqual(1,settlement.RawAward);Assert.AreEqual(1,selection.LineWin);Assert.AreEqual(1.25f,selection.TotalWin);
        for(int c=0;c<5;c++)for(int r=0;r<3;r++)board[c,r]=10;
        settlement.Evaluate(board,1);selection.Capture(settlement,(c,r)=>board[c,r],5,1);
        Assert.IsTrue(selection.HasReward);Assert.IsFalse(selection.RequestsLineSound);Assert.AreEqual(0,selection.Count);
        Assert.AreEqual(RecoveredSlotWinType.Super,selection.BigWin);
    }
}
