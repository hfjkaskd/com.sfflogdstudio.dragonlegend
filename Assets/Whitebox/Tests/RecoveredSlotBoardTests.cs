using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using Random = UnityEngine.Random;

public sealed class RecoveredSlotBoardTests
{
    private static RecoveredGameplayRules Rules()
    {
        var ordinary = new List<int>{100};
        var more = new List<int>{0,0,0,100};
        return new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
            Gimrol = new GimrolPoro {
                Rggl1 = ordinary, Rggl2 = ordinary, Rggl3 = ordinary, Rggl4 = ordinary, Rggl5 = ordinary,
                RgglKilp1 = ordinary, RgglKilp2 = ordinary, RgglKilp3 = ordinary,
                RgglKilp4 = ordinary, RgglKilp5 = ordinary,
                MorgKilp1 = more, MorgKilp2 = more, MorgKilp3 = more, MorgKilp4 = more, MorgKilp5 = more,
                Lingg = new List<int>{30},
                J3 = new List<int>{0,0,0,0,0,0,0,3},
                J4 = new List<int>{0,0,0,0,0,0,0,6},
                J5 = new List<int>{0,0,0,0,0,0,0,9}
            }
        });
    }

    [TestCase(0,4)] [TestCase(1,3)] [TestCase(2,5)] [TestCase(3,0)] [TestCase(-1,0)]
    public void GuaranteedWildIndexHasOriginalMapping(int index, int expected)
    {
        var state = Random.state;
        try
        {
            var board = new RecoveredSlotBoard(Rules()); board.FillBase();
            board.ApplyGuaranteedWildIndex(index, new System.Random(17));
            int fullColumns = 0;
            for (int c = 0; c < 5; c++)
            {
                int symbol = board.GetSymbol(c,0);
                for (int r = 0; r < 3; r++)
                {
                    Assert.AreEqual(symbol, board.GetSymbol(c,r));
                    Assert.IsFalse(board.IsReserved(c,r));
                }
                if (symbol == 7) fullColumns++;
            }
            Assert.AreEqual(expected, fullColumns);
        }
        finally { Random.state = state; }
    }

    [Test]
    public void EqualRandomKeysKeepSourceOrder()
    {
        var board = new RecoveredSlotBoard(Rules());
        board.ApplyGuaranteedWildIndex(1, new EqualKeys());
        for (int c = 0; c < 5; c++) Assert.AreEqual(c < 3 ? 7 : 0, board.GetSymbol(c,0));
    }
    private sealed class EqualKeys : System.Random { public override int Next() => 10; }

    [Test]
    public void MoreWildReservesRowsAndNormalStageClearsReservations()
    {
        var state = Random.state;
        try
        {
            Random.InitState(71);
            var board = new RecoveredSlotBoard(Rules());
            board.FillBase(); board.ApplyRegularWilds(1);
            int count = 0;
            for (int c = 0; c < 5; c++) for (int r = 0; r < 3; r++)
                if (board.IsReserved(c,r)) { count++; Assert.AreEqual(7,board.GetSymbol(c,r)); }
            Assert.Greater(count, 0);
            board.FillBase(); board.ApplyRegularWilds(0);
            for (int c = 0; c < 5; c++) for (int r = 0; r < 3; r++)
            { Assert.IsFalse(board.IsReserved(c,r)); Assert.AreEqual(0,board.GetSymbol(c,r)); }
        }
        finally { Random.state = state; }
    }

    [Test]
    public void GuaranteedBoardConnectsToSettlementAndStopTrigger()
    {
        var rules = Rules(); var board = new RecoveredSlotBoard(rules);
        var settlement = new RecoveredSlotSettlement(rules);
        board.ApplyGuaranteedWildIndex(2);
        board.Settle(settlement, 10);
        Assert.AreEqual(243,settlement.WinningLines.Count);
        Assert.AreEqual(729f,settlement.RawAward);
        Assert.AreEqual(1,board.SpeedRoll(0));
    }
}
