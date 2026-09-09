using System;
using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredFreeBallScanTests
{
    [UnityTest]
    public IEnumerator TwoStoppedBallsWaitForExternalRewardAndShareCoinLedgerInColumnOrder()
    {
        var random=UnityEngine.Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=new RrggiomgPoro{
            QoinOmoinrKgiitr=new List<int>{1000000},RollOmoinrKgiitr=new List<int>{0,0,1000000},RollRipgKgiitr=new List<int>{1000000}}});
        var result=new RecoveredFreeSpinResult(rules);result.Begin(new[]{3});while(result.IsGenerating)result.Step();
        var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        root.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),result);
        var npc=Object.Instantiate(Resources.Load<RecoveredNpcPresentation>("RecoveredUI/Npc"));
        var collection=Object.Instantiate(Resources.Load<RecoveredBonusCollection>("RecoveredUI/BonusCollection"));
        var target=new GameObject("Ball scan destination");target.transform.position=new Vector3(0,3,0);
        var downWin=Object.Instantiate(Resources.Load<RecoveredDownWinText>("RecoveredUI/DownWinText"));downWin.Bind(0);
        var bottom=new GameObject("Free collection bottom",typeof(RectTransform));int collected=0;
        int saves=0,done=0,requests=0;Action<float> pending=null;Exception npcError=null;npc.Failed+=error=>npcError=error;
        var player=new RecoveredPlayerProgress(rules,()=>saves++,new PlayerData{BonusArea=new List<int>{0,0,0,0,0},GreenCount=20}){GameSlotType=RecoveredSlotType.Free,TotalFreeSpinWin=10};
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            var expected=new List<RecoveredReelView>();
            for(int column=0;column<5;column++) {
                for(int row=0;row<3;row++) {
                    var reel=root.At(column,row);root.Specials.ApplyStoppedResult(reel,column,row);
                    if(result.GetSymbol(column,row)==11)expected.Add(reel);
                }
                root.ColumnAt(column).ShowFreeEffects();
            }
            Assert.AreEqual(2,expected.Count);
            root.CoinScan.Bind(rules,player,result,collection,()=>0);
            root.RewardCollect.Bind(result,player,downWin,(RectTransform)bottom.transform,300);
            root.RewardCollect.Completed+=()=>collected++;
            root.BallScan.Bind(result,player,npc,target.transform,npc.transform.Find("PlayFire"),()=>0,(type,position,callback)=>{
                Assert.AreEqual(0,type);Assert.AreEqual(0,root.Specials.FlightBallCount,"Clone returns before small-game handoff.");
                requests++;pending=callback;
            });
            root.BallScan.Completed+=()=>done++;root.BallScan.Begin();
            for(int ball=0;ball<2;ball++) {
                for(int i=0;i<100&&pending==null;i++)yield return null;
                Assert.IsNull(root.BallScan.Error);Assert.IsNull(npcError);Assert.IsNotNull(pending);
                Assert.AreEqual(ball+1,requests);Assert.IsTrue(root.BallScan.IsRunning);Assert.AreEqual(0,done);
                Assert.AreEqual(ball,root.CoinScan.Rewards.Count);
                // No synthetic success: even after many frames this row awaits its consumer.
                for(int i=0;i<20;i++)yield return null;Assert.AreEqual(ball+1,requests);Assert.AreEqual(0,done);
                var reply=pending;pending=null;reply(30+ball);
                Assert.AreEqual(30+ball,root.CoinScan.Rewards[expected[ball].gameObject]);
                Assert.IsTrue(root.Specials.CurrentStoppedBall(expected[ball]).RewardPresentation.IsAnimating);
            }
            for(int i=0;i<25&&done==0;i++)yield return null;
            Assert.AreEqual(1,done);Assert.IsFalse(root.BallScan.IsRunning);Assert.IsNull(root.BallScan.Error);
            Assert.AreEqual(71,player.TotalFreeSpinWin);Assert.AreEqual(0,saves);Assert.AreEqual(20,player.GreenCount);
            Assert.IsTrue(root.RewardCollect.IsRunning,"Ball scan completion starts actual collection.");
            for(int i=0;i<80&&collected==0;i++)yield return null;
            Assert.IsNull(root.RewardCollect.Error);Assert.AreEqual(1,collected);
            Assert.AreEqual(61,root.RewardCollect.FreeReward);Assert.AreEqual(0,root.RewardCollect.CoinReward);
            Assert.AreEqual(20,player.GreenCount);Assert.AreEqual(2,saves,"Even zero coin credit invokes the original setter.");
            Assert.AreEqual(71,player.TotalFreeSpinWin);
        } finally {Object.Destroy(root.gameObject);Object.Destroy(npc.gameObject);Object.Destroy(collection.gameObject);Object.Destroy(target);Object.Destroy(downWin.gameObject);Object.Destroy(bottom);UnityEngine.Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }
}
