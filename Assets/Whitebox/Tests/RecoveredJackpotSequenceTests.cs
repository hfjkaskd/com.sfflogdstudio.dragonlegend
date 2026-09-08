using System;
using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredJackpotSequenceTests
{
    [TestCase(0)][TestCase(1)][TestCase(2)]
    public void FewerThanThreeWildColumnsContinueSynchronouslyWithoutSideEffects(int count)
    {
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig());
        var progress=new RecoveredPlayerProgress(rules,()=>Assert.Fail("No task save"),new PlayerData());
        var sequence=new RecoveredJackpotSequence(new RecoveredRewardBranches(rules,progress),1.5f);int completed=0;
        sequence.WinRequested+=_=>Assert.Fail("No win");sequence.PauseMusicRequested+=()=>Assert.Fail("No pause");
        sequence.Begin(count,(a,b,c)=>Assert.Fail("No popup"),()=>completed++);
        Assert.AreEqual(1,completed);Assert.IsFalse(sequence.IsRunning);Assert.AreEqual(RecoveredJackpotType.None,sequence.Type);
    }
    [UnityTest]
    public IEnumerator EveryTierSnapshotsAfterSaveWaitsScaledTimeAndOnlyContinuesAfterCallback()
    {
        float scale=Time.timeScale,delta=Time.captureDeltaTime;RecoveredJackpotSequence sequence=null;
        try
        {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            for(int wild=3;wild<=5;wild++)
            {
                var order=new List<string>();var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig());
                RecoveredPlayerProgress progress=null;progress=new RecoveredPlayerProgress(rules,()=>{
                    order.Add("save");progress.GrandJackPotReward=500;progress.MajorJackPotReward=400;progress.MiniJackPotReward=300;
                },new PlayerData());
                sequence=new RecoveredJackpotSequence(new RecoveredRewardBranches(rules,progress),1.5f);
                float expected=wild==3?300:wild==4?400:500;int finished=0;Action<float> complete=null;
                sequence.Failed+=e=>Assert.Fail(e.ToString());
                sequence.WinRequested+=tier=>{
                    Assert.AreEqual(wild==3?RecoveredJackpotType.Minor:wild==4?RecoveredJackpotType.Major:RecoveredJackpotType.Grand,tier);
                    Assert.AreEqual(expected,sequence.CapturedReward);order.Add("win");
                    progress.GrandJackPotReward=progress.MajorJackPotReward=progress.MiniJackPotReward=-1;
                };
                sequence.PauseMusicRequested+=()=>order.Add("pause");sequence.Sound1Requested+=s=>order.Add(s);sequence.StopSound1Requested+=()=>order.Add("stop");
                sequence.Begin(wild,(tier,amount,callback)=>{Assert.AreEqual(expected,amount);complete=callback;order.Add("popup");},()=>{finished++;order.Add("symbols");});
                CollectionAssert.AreEqual(new[]{"save","win","pause","dragon3"},order);
                Time.timeScale=0;for(int i=0;i<6;i++)yield return null;Assert.IsNull(complete);
                Time.timeScale=1;float start=Time.time;
                for(int i=0;i<45&&complete==null;i++)yield return null;
                Assert.IsNotNull(complete);Assert.GreaterOrEqual(Time.time-start,1.5f-.001f);
                CollectionAssert.AreEqual(new[]{"save","win","pause","dragon3","stop","popup"},order);
                for(int i=0;i<5;i++)yield return null;Assert.AreEqual(0,finished);Assert.IsTrue(sequence.IsRunning);
                complete(-999);Assert.AreEqual(0,finished,"Callback only sets the predicate flag");
                for(int i=0;i<3;i++)yield return null;
                Assert.AreEqual(1,finished);Assert.IsFalse(sequence.IsRunning);
            }
        }
        finally{sequence?.Cancel();Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }
    [UnityTest]
    public IEnumerator CancellationInvalidatesDelayedShowAndOldFlightCallbacks()
    {
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig());
        var data=new PlayerData{PlayerTaskDatas=null};var progress=new RecoveredPlayerProgress(rules,()=>{},data);
        var sequence=new RecoveredJackpotSequence(new RecoveredRewardBranches(rules,progress),.1f);
        try
        {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            sequence.Begin(3,(a,b,c)=>Assert.Fail("Cancelled delay"),()=>Assert.Fail("Cancelled round"));sequence.Cancel();
            for(int i=0;i<4;i++)yield return null;
            Action<float> old=null,current=null;int completed=0;
            sequence.Begin(3,(a,b,c)=>old=c,()=>Assert.Fail("Old round"));
            for(int i=0;i<5&&old==null;i++)yield return null;Assert.IsNotNull(old);sequence.Cancel();
            sequence.Begin(4,(a,b,c)=>current=c,()=>completed++);
            for(int i=0;i<5&&current==null;i++)yield return null;Assert.IsNotNull(current);
            old(1);for(int i=0;i<3;i++)yield return null;Assert.AreEqual(0,completed);
            current(1);for(int i=0;i<3;i++)yield return null;Assert.AreEqual(1,completed);
        }
        finally{sequence.Cancel();Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }
}
