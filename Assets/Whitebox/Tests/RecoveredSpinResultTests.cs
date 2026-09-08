using System;
using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

public sealed class RecoveredSpinResultTests
{
    [UnityTest]
    public IEnumerator RealSnapshotGeneratesConsecutiveResults()
    {
        var loader = new ConfigSnapshotLoader();
        yield return loader.Load("RecoveredConfig/Remote/cp_default_1.json");
        var state=Random.state;
        try
        {
            Random.InitState(193);
            var rules=new RecoveredGameplayRules(loader.Value);
            var spin=new RecoveredSpinResult(rules,new RecoveredSlotSettlement(rules));
            for(int i=0;i<12;i++)
            {
                int callbacks=0;
                spin.Begin(i==0,1,0,new[]{0,0,0,0,0},_=>callbacks++,_=>callbacks++);
                Finish(spin);
                Assert.AreEqual(2,callbacks);
                Assert.IsFalse(float.IsNaN(spin.Settlement.RawAward));
                for(int c=0;c<5;c++) for(int r=0;r<3;r++)
                    Assert.That(spin.Board.GetSymbol(c,r),Is.InRange(0,10));
            }
        }
        finally {Random.state=state;}
    }
    private static RecoveredGameplayRules Rules()
    {
        var weights = new List<int>{100};
        return new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
            Gimrol = new GimrolPoro {
                Rggl1=weights,Rggl2=weights,Rggl3=weights,Rggl4=weights,Rggl5=weights,
                RgglKilp1=weights,RgglKilp2=weights,RgglKilp3=weights,RgglKilp4=weights,RgglKilp5=weights,
                KilpGpinQP=new List<int>{3},KilpGpinKgiitr=weights,Lingg=new List<int>{30},
                J3=new List<int>{0,0,0,0,0,0,0,3},J4=new List<int>{0,0,0,0,0,0,0,6},
                J5=new List<int>{0,0,0,0,0,0,0,9}
            },
            Ronig = new RonigPoro {MinGpin=new List<int>{3},QoinGpinKgiitr=new List<int>{100,100}},
            Rrggiomg = new RrggiomgPoro {GpinGqorrgrRonpom=weights}
        });
    }
    private static void Finish(RecoveredSpinResult spin)
    {
        int steps = 0;
        while (spin.Step()) Assert.Less(++steps,1000,"Result generation failed to finish");
    }

    [Test]
    public void GuideSkipsScatterAndOrdinarySpinUsesDoubleBonusIncrement()
    {
        var state = Random.state;
        try
        {
            Random.InitState(89);
            var rules = Rules(); var settlement = new RecoveredSlotSettlement(rules);
            var spin = new RecoveredSpinResult(rules,settlement) {ForceFreeSpin=true};
            var events = new List<string>(); var areas = new[]{2,2,2,2,2};
            spin.Begin(true,1,0,areas,speed=>events.Add("start"),scatter=>events.Add("scatter"));
            Finish(spin);
            CollectionAssert.AreEqual(new[]{"start","scatter"},events);
            Assert.IsTrue(spin.FirstFreeReward); Assert.IsTrue(spin.ForceFreeSpin);
            Assert.AreEqual(0,spin.ScatterCount); Assert.AreEqual(0,spin.WildCounter);
            Assert.AreEqual(1,spin.BonusCounter);
            spin.Begin(false,1,0,areas); Finish(spin);
            Assert.IsFalse(spin.FirstFreeReward); Assert.IsFalse(spin.ForceFreeSpin);
            Assert.That(spin.ScatterCount,Is.InRange(3,5));
            Assert.AreEqual(0,spin.BonusCounter); // 1 + 1 + 1 reaches minimum 3
            Assert.AreEqual(1,spin.WildCounter);
            spin.Begin(false,1,0,areas); Finish(spin);
            Assert.AreEqual(2,spin.BonusCounter);
            spin.Begin(false,1,0,areas); Finish(spin);
            Assert.AreEqual(3,spin.BonusCounter); // Wild branch skips second increment/reset
            Assert.AreEqual(0,spin.WildCounter);
        }
        finally { Random.state=state; }
    }

    [Test]
    public void BonusRegionUsesRandomIndexAsRowAndComplementAsForbiddenColumns()
    {
        var board = new RecoveredSlotBoard(Rules());
        var draws = new Queue<int>(new[]{1,0});
        board.BeginSingleSymbol(1,new List<int>(),10);
        while(board.StepSingleSymbol((min,max)=>draws.Dequeue())) {}
        board.BeginGuaranteedBonus(1,new[]{1},new System.Random(7),(min,max)=>0);
        Assert.AreEqual(9,board.GetSymbol(1,0)); // overwrites reserved row zero
        Assert.IsFalse(board.StepSingleSymbol());
        board.BeginGuaranteedBonus(2,new[]{1},new System.Random(7),(min,max)=>0);
        Assert.IsTrue(board.StepSingleSymbol((min,max)=>0)); // outside region forbidden
        Assert.IsTrue(board.IsPlacingSingleSymbol);
        int draw=0;
        Assert.IsTrue(board.StepSingleSymbol((min,max)=>draw++==0 ? 1 : 0));
        Assert.IsFalse(board.StepSingleSymbol());
    }

    [Test]
    public void ConcurrentBeginIsRejectedAndCallbacksFireOnce()
    {
        var state = Random.state;
        try
        {
            var rules=Rules(); var spin=new RecoveredSpinResult(rules,new RecoveredSlotSettlement(rules));
            int calls=0;
            spin.Begin(true,1,0,new[]{2,2,2,2,2},_=>calls++,_=>calls++);
            Assert.Throws<InvalidOperationException>(()=>spin.Begin(false,1,0,new int[5]));
            Finish(spin); Assert.AreEqual(2,calls);
            Assert.IsFalse(spin.Step()); Assert.AreEqual(2,calls);
        }
        finally {Random.state=state;}
    }
}
