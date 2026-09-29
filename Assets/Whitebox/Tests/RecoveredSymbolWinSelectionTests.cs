using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredSymbolWinSelectionTests
{
    private static RecoveredGameplayRules Rules(params int[] thresholds)
        =>new RecoveredGameplayRules(Configuration(thresholds));
    private static GoldenDragonAutoGenConfig Configuration(params int[] thresholds)
        =>new GoldenDragonAutoGenConfig {
            Qonrii=new QonriiPoro{Riikin=new List<int>(thresholds)},
            Gimrol=new GimrolPoro{Lingg=new List<int>{30},J3=new List<int>{30,30,30,30,30,30,30,3},
                J4=new List<int>{60,60,60,60,60,60,60,6},J5=new List<int>{90,90,90,90,90,90,90,9}}
        };
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

    private static RecoveredGameplayRules PercentageRules()
    {
        var config=Configuration(2,4,6);
        config.Qonrii.BigWinBetPercent=new List<int>{20,50,100};
        return new RecoveredGameplayRules(config);
    }

    [TestCase(199.99f,RecoveredSlotWinType.None)]
    [TestCase(200,RecoveredSlotWinType.Big)]
    [TestCase(499.99f,RecoveredSlotWinType.Big)]
    [TestCase(500,RecoveredSlotWinType.Mega)]
    [TestCase(999.99f,RecoveredSlotWinType.Mega)]
    [TestCase(1000,RecoveredSlotWinType.Super)]
    public void PercentageBoundariesUseRawCentsAndOverrideLegacyMultipliers(float amount,RecoveredSlotWinType expected)
        =>Assert.AreEqual(expected,PercentageRules().GetBigWin(amount,1000));

    [Test]
    public void PercentageThresholdsFollowTheCurrentBetAndPreserveFractionalThresholds()
    {
        var rules=PercentageRules();
        Assert.AreEqual(RecoveredSlotWinType.Big,rules.GetBigWin(750,2000));
        Assert.AreEqual(RecoveredSlotWinType.Mega,rules.GetBigWin(750,1000));
        Assert.AreEqual(RecoveredSlotWinType.Super,rules.GetBigWin(750,500));
        Assert.AreEqual(RecoveredSlotWinType.None,rules.GetBigWin(200,1001),"20% of 1001 is 200.2, not integer-truncated 200.");
        Assert.AreEqual(RecoveredSlotWinType.Big,rules.GetBigWin(200.25f,1001));
        Assert.AreEqual(RecoveredSlotWinType.Big,rules.GetBigWin(500,1001));
        Assert.AreEqual(RecoveredSlotWinType.Mega,rules.GetBigWin(500.5f,1001));
        Assert.AreEqual(RecoveredSlotWinType.Mega,rules.GetBigWin(1000,1001));
        Assert.AreEqual(RecoveredSlotWinType.Super,rules.GetBigWin(1001,1001));
    }

    [TestCase("{\"Riikin\":[2,4,6]}")]
    [TestCase("{\"Riikin\":[2,4,6],\"BigWinBetPercent\":[]}")]
    [TestCase("{\"Riikin\":[2,4,6],\"BigWinBetPercent\":null}")]
    public void MissingOrEmptyJsonPercentageFieldRetainsLegacyThresholds(string section)
    {
        var config=UnityEngine.JsonUtility.FromJson<GoldenDragonAutoGenConfig>("{\"Qonrii\":"+section+"}");
        var rules=new RecoveredGameplayRules(config);
        Assert.AreEqual(RecoveredSlotWinType.None,rules.GetBigWin(1999.99f,1000));
        Assert.AreEqual(RecoveredSlotWinType.Big,rules.GetBigWin(2000,1000));
        Assert.AreEqual(RecoveredSlotWinType.Big,rules.GetBigWin(3999.99f,1000));
        Assert.AreEqual(RecoveredSlotWinType.Mega,rules.GetBigWin(4000,1000));
        Assert.AreEqual(RecoveredSlotWinType.Mega,rules.GetBigWin(5999.99f,1000));
        Assert.AreEqual(RecoveredSlotWinType.Super,rules.GetBigWin(6000,1000));
    }

    [TestCase("{\"Riikin\":[2,4,6],\"BigWinBetPercent\":[20,50,100]}")]
    [TestCase("{\"BigWinBetPercent\":[20,50,100]}")]
    public void JsonPercentageFieldIsDeserializedAndTakesPriority(string section)
    {
        var config=UnityEngine.JsonUtility.FromJson<GoldenDragonAutoGenConfig>("{\"Qonrii\":"+section+"}");
        CollectionAssert.AreEqual(new[]{20,50,100},config.Qonrii.BigWinBetPercent);
        var rules=new RecoveredGameplayRules(config);
        Assert.AreEqual(RecoveredSlotWinType.None,rules.GetBigWin(199.99f,1000));
        Assert.AreEqual(RecoveredSlotWinType.Big,rules.GetBigWin(200,1000));
        Assert.AreEqual(RecoveredSlotWinType.Mega,rules.GetBigWin(500,1000));
        Assert.AreEqual(RecoveredSlotWinType.Super,rules.GetBigWin(1000,1000));
    }

    [TestCase(99.99f,RecoveredSlotWinType.None)]
    [TestCase(100,RecoveredSlotWinType.Big)]
    [TestCase(400,RecoveredSlotWinType.Mega)]
    [TestCase(900,RecoveredSlotWinType.Super)]
    public void PercentageSelectionClassifiesLinePlusCoinWithoutChangingEitherAward(float coinAward,RecoveredSlotWinType expected)
    {
        var rules=PercentageRules();var settlement=new RecoveredSlotSettlement(rules);var board=new int[5,3];
        for(int c=0;c<5;c++)for(int r=0;r<3;r++)board[c,r]=10;
        board[0,0]=board[1,0]=board[2,0]=7;board[3,0]=board[4,0]=1;
        settlement.Evaluate(board,1000);
        Assert.AreEqual(100,settlement.RawAward);
        var selection=new RecoveredSymbolWinSelection(rules);
        selection.Capture(settlement,(c,r)=>board[c,r],coinAward,1000);
        Assert.AreEqual(100,selection.LineWin);
        Assert.AreEqual(100+coinAward,selection.TotalWin,.0001f);
        Assert.AreEqual(expected,selection.BigWin);
        Assert.AreEqual(100,settlement.RawAward,"Popup classification must not modify the line award.");
        Assert.AreEqual(5,selection.Count);Assert.IsTrue(selection.RequestsLineSound);
    }

    [TestCase(199.99f,RecoveredSlotWinType.None)]
    [TestCase(200,RecoveredSlotWinType.Big)]
    [TestCase(500,RecoveredSlotWinType.Mega)]
    [TestCase(1000,RecoveredSlotWinType.Super)]
    public void PercentageSelectionAlsoClassifiesCoinOnlyWins(float coinAward,RecoveredSlotWinType expected)
    {
        var rules=PercentageRules();var settlement=new RecoveredSlotSettlement(rules);var board=new int[5,3];
        for(int c=0;c<5;c++)for(int r=0;r<3;r++)board[c,r]=10;
        settlement.Evaluate(board,1000);
        var selection=new RecoveredSymbolWinSelection(rules);
        selection.Capture(settlement,(c,r)=>{Assert.Fail("A coin-only win has no winning line cells.");return 0;},coinAward,1000);
        Assert.AreEqual(0,selection.LineWin);Assert.AreEqual(coinAward,selection.TotalWin);
        Assert.AreEqual(expected,selection.BigWin);Assert.AreEqual(0,selection.Count);
        Assert.IsTrue(selection.HasReward);Assert.IsFalse(selection.RequestsLineSound);
    }
}
