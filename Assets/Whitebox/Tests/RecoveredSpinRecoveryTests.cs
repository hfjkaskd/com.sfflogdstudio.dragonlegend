using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredSpinRecoveryTests
{
    private static GoldenDragonAutoGenConfig Config(string type="default")=>new GoldenDragonAutoGenConfig{
        Qonrii=new QonriiPoro{MojGping=new List<int>{100,30},Ripg=new List<string>{type},
            Lgtgl=new List<int>{1,2},GpinQP=new List<int>{60,30}}};

    [Test]
    public void OfflineRemainderIsPreservedAndBothSavesPrecedeFinalFullTimestamp()
    {
        var rules=new RecoveredGameplayRules(Config());var data=new PlayerData{Level=1,SpinCount=97,LastSpinTime=1000};
        var saves=new List<(int,int)>();var player=new RecoveredPlayerProgress(rules,()=>saves.Add((data.SpinCount,data.LastSpinTime)),data);
        using(var recovery=new RecoveredSpinRecovery(rules,player,data))
        {
            recovery.Initialize(1125);Assert.AreEqual(99,player.SpinCount);Assert.AreEqual(1120,data.LastSpinTime);
            CollectionAssert.AreEqual(new[]{(99,1120),(99,1120)},saves);Assert.AreEqual(55,recovery.RemainingSeconds);
            recovery.Advance(54,1179);Assert.AreEqual(99,player.SpinCount);
            recovery.Advance(1,1180);Assert.AreEqual(100,player.SpinCount);Assert.IsFalse(recovery.IsRunning);
            Assert.AreEqual(4,saves.Count);Assert.AreEqual((100,1180),saves[3]);
        }
        saves.Clear();data.SpinCount=99;data.LastSpinTime=1000;
        using(var recovery=new RecoveredSpinRecovery(rules,player,data))
        {
            recovery.Initialize(1125);Assert.AreEqual(1125,data.LastSpinTime);
            CollectionAssert.AreEqual(new[]{(100,1120),(100,1120)},saves);
            Assert.IsFalse(recovery.IsRunning);
        }
    }
    [Test]
    public void OnlineTickReadsLiveCountAndNextLevelCooldownAndPausesWithScaledTime()
    {
        var config=Config();var rules=new RecoveredGameplayRules(config);var data=new PlayerData{Level=1,SpinCount=80,LastSpinTime=1000};
        int saves=0;var player=new RecoveredPlayerProgress(rules,()=>saves++,data);
        using(var recovery=new RecoveredSpinRecovery(rules,player,data))
        {
            int shown=-1;int? seconds=null;recovery.DisplayRequested+=(count,time)=>{shown=count;seconds=time;};
            recovery.Initialize(1059);Assert.AreEqual(1,recovery.RemainingSeconds);
            recovery.Advance(0,9000);Assert.AreEqual(80,player.SpinCount);
            data.SpinCount=81;data.Level=2;recovery.Advance(1,9001);
            Assert.AreEqual(82,player.SpinCount);Assert.AreEqual(9001,data.LastSpinTime);
            Assert.AreEqual(2,saves);Assert.AreEqual(30,seconds);Assert.AreEqual(82,shown);
            player.SetSpinCount(999);Assert.IsFalse(recovery.IsRunning);Assert.IsNull(seconds);Assert.AreEqual(100,shown);
            recovery.Advance(100,9101);Assert.AreEqual(3,saves);
        }
    }
    [TestCase("organic",20,1000,false,0)]
    [TestCase("default",100,1000,false,0)]
    [TestCase("default",20,1010,true,70)]
    public void ProfileFullCountAndFutureTimestampFollowNativeBranches(string type,int count,int last,bool running,int remaining)
    {
        var rules=new RecoveredGameplayRules(Config(type));var data=new PlayerData{Level=1,SpinCount=count,LastSpinTime=last};
        int saves=0;var player=new RecoveredPlayerProgress(rules,()=>saves++,data);
        var recovery=new RecoveredSpinRecovery(rules,player,data);recovery.Initialize(1000);
        Assert.AreEqual(running,recovery.IsRunning);Assert.AreEqual(remaining,recovery.RemainingSeconds);Assert.AreEqual(0,saves);
        recovery.Dispose();recovery.Advance(1000,2000);recovery.Begin(1);Assert.IsFalse(recovery.IsRunning);Assert.AreEqual(count,player.SpinCount);
    }
}
