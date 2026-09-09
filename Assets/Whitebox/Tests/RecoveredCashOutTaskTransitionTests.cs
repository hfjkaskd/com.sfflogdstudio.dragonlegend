using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredCashOutTaskTransitionTests
{
    private static RgpggmPoro Config()=>new RgpggmPoro {
        Qogt=new List<int>{100,200,300,400,500,600},
        Rimg7=new List<int>{0,0,0,0,0,0},
        Rogk1tir=new List<int>{0,0,0,9,0,0},Rogk2tir=new List<int>{0,0,0,0,0,0},
        Rogk3tir=new List<int>{0,0,0,0,0,0},Rogk4tir=new List<int>{0,0,0,0,0,0},
        Rogk5tir=new List<int>{0,0,0,0,0,0},Rogk6tir=new List<int>{0,0,0,0,0,0},
        Rogk1roil=new List<int>{0,0,0,0,0,0},Rogk2roil=new List<int>{0,0,0,0,0,0},
        Rogk3roil=new List<int>{0,0,0,0,0,0},Rogk4roil=new List<int>{7,0,0,0,0,0},
        Rogk5roil=new List<int>{0,0,0,0,0,0},Rogk6roil=new List<int>{0,0,0,0,0,0}
    };
    [TestCase(0,3,3)]
    [TestCase(1,3,3)]
    [TestCase(2,3,3)]
    [TestCase(3,1000,6)]
    [TestCase(4,1000,6)]
    [TestCase(5,1000,6)]
    [TestCase(6,1000,6)]
    [TestCase(-1,1000,6)]
    [TestCase(1000,1000,6)]
    public void SkipsZeroTasksAndPreservesDistinctTerminalSteps(int current,int success,int failure)
    {
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rgpggm=Config()});
        Assert.AreEqual(success,rules.GetNextCashOutTaskStep(0,current,true));
        Assert.AreEqual(failure,rules.GetNextCashOutTaskStep(0,current,false));
    }
    [Test]
    public void SuccessUsesTransposedArgumentsAndOriginalTierFallback()
    {
        var config=Config();config.Rogk1tir[3]=0;config.Rogk2tir[4]=2;
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rgpggm=config});
        Assert.AreEqual(4,rules.GetNextCashOutTaskStep(1,0,true));
        Assert.AreEqual(1000,rules.GetNextCashOutTaskStep(0,0,true));
        config.Qogt=new List<int>{100};config.Rogk1tir[0]=2;
        Assert.AreEqual(1,rules.GetNextCashOutTaskStep(0,0,true),"Candidate tier outside config falls back to tier zero.");
    }
    [Test]
    public void NegativeTasksAreSkippedAndFirstPositiveCandidateWins()
    {
        var config=Config();config.Rogk2roil[0]=-7;config.Rogk3roil[0]=2;config.Rogk6roil[0]=3;
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rgpggm=config});
        Assert.AreEqual(2,rules.GetNextCashOutTaskStep(0,0,false));
        Assert.AreEqual(3,rules.GetNextCashOutTaskStep(0,2,false));
        Assert.AreEqual(5,rules.GetNextCashOutTaskStep(0,3,false));
    }
    [Test]
    public void FailureStepFiveStillReadsTimeSevenWithoutTierFallback()
    {
        var config=Config();var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rgpggm=config});
        config.Rimg7[0]=19;Assert.AreEqual(6,rules.GetNextCashOutTaskStep(0,5,false));
        config.Rimg7[0]=-1;Assert.AreEqual(6,rules.GetNextCashOutTaskStep(0,5,false));
        Assert.Throws<System.ArgumentOutOfRangeException>(()=>rules.GetNextCashOutTaskStep(99,5,false));
    }
    [Test]
    public void CallbackMutatesCapturedRecordBeforeSavingWithoutDebitingOrReselecting()
    {
        var data=new PlayerData{GreenCount=123};var captured=new PlayerCashOutData{id=0,type=3,step=2,count=99,time=11,isCashout=true};
        var duplicate=new PlayerCashOutData{id=0,step=4,count=12};data.PlayerCashOutDatas.Add(duplicate);data.PlayerCashOutDatas.Add(captured);
        int saves=0;var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rgpggm=Config()});
        var player=new RecoveredPlayerProgress(rules,()=>{saves++;Assert.AreEqual(5,captured.step);Assert.AreEqual(0,captured.count);Assert.AreEqual(777,captured.time);},data);
        player.ApplyCashOutTaskStep(captured,5,777);
        Assert.AreEqual(1,saves);Assert.AreEqual(4,duplicate.step);Assert.AreEqual(12,duplicate.count);
        Assert.AreEqual(3,captured.type);Assert.IsTrue(captured.isCashout);Assert.AreEqual(123,data.GreenCount);
        data.PlayerCashOutDatas.Remove(captured);player.ApplyCashOutTaskStep(captured,5,777);
        Assert.AreEqual(2,saves);Assert.AreEqual(1,data.PlayerCashOutDatas.Count);
    }
}
