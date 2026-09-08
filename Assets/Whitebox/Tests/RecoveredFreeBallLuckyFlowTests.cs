using System;
using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
using Random=UnityEngine.Random;

public sealed class RecoveredFreeBallLuckyFlowTests
{
    [UnityTest]
    public IEnumerator TwoStoppedBallsRouteToActualLuckyClaimAndResumeScanWithPaidRewards()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=Random.state;
        Scene scene=default;AsyncOperation unload=null;RecoveredFreeReels reels=null;RecoveredNpcPresentation npc=null;RecoveredFreeLuckyGame game=null;
        GameObject target=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;Random.InitState(371);
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry entry=null;foreach(var root in scene.GetRootGameObjects()){var value=root.GetComponentInChildren<GameEntry>();if(value!=null)entry=value;}
            Assert.IsNotNull(entry);float deadline=Time.realtimeSinceStartup+5;
            while(entry.CashFlight==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(entry.CashFlight);
            // Deterministic branch fixture; all entry, claim, flight and ledger consumers are real.
            var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=new RrggiomgPoro{
                QoinOmoinrKgiitr=new List<int>{1000000},RollOmoinrKgiitr=new List<int>{0,0,1000000},RollRipgKgiitr=new List<int>{1000000},
                RollGlorg=new List<int>{0,0,0},RollKtggl=new List<int>{0,0,0},RollRrgogirg=new List<int>{0,0,0},RollLiqki=new List<int>{1000000,1000000,1000000}}});
            var result=new RecoveredFreeSpinResult(rules);result.Begin(new[]{3});while(result.IsGenerating)result.Step();
            reels=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
            reels.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),result);
            npc=Object.Instantiate(Resources.Load<RecoveredNpcPresentation>("RecoveredUI/Npc"));
            target=new GameObject("Ball flow destination");target.transform.position=new Vector3(0,3,0);
            game=Object.Instantiate(Resources.Load<RecoveredFreeLuckyGame>("RecoveredUI/FreeLuckyGame"),entry.transform,false);
            game.Bind(entry.PlayerProgress,entry.Rules,entry.Ads,entry.CashFlight,entry.transform,entry.CurrentProfile.isA,entry.CurrentProfile.languageType);
            entry.PlayerProgress.GameSlotType=RecoveredSlotType.Free;
            float initialBalance=entry.PlayerProgress.GreenCount,initialTotal=entry.PlayerProgress.TotalFreeSpinWin,paid=0;
            int requests=0,done=0;Exception npcError=null;npc.Failed+=error=>npcError=error;
            Action<Vector3,Action<float>> unexpected=(p,c)=>Assert.Fail("The seeded fixture selected an unexpected branch.");
            var router=new RecoveredFreeSmallGameRouter(rules,entry.PlayerProgress,unexpected,unexpected,unexpected,(position,callback)=>{
                requests++;Assert.AreEqual(0,reels.Specials.FlightBallCount);game.Begin(position,callback);
            });
            var expected=new List<RecoveredReelView>();
            for(int column=0;column<5;column++) {
                for(int row=0;row<3;row++) {
                    var reel=reels.At(column,row);reels.Specials.ApplyStoppedResult(reel,column,row);
                    if(result.GetSymbol(column,row)==11)expected.Add(reel);
                }
                reels.ColumnAt(column).ShowFreeEffects();
            }
            Assert.AreEqual(2,expected.Count);
            reels.CoinScan.Bind(entry.Rules,entry.PlayerProgress,result,entry.Playfield.BonusCollection,()=>entry.CurrentProfile.languageType);
            reels.BallScan.Bind(result,entry.PlayerProgress,npc,target.transform,npc.transform.Find("PlayFire"),()=>entry.CurrentProfile.languageType,router.Open);
            reels.BallScan.Completed+=()=>done++;reels.BallScan.Begin();
            for(int ball=0;ball<2;ball++) {
                deadline=Time.realtimeSinceStartup+5;
                while(!game.Popup.gameObject.activeSelf&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsNull(reels.BallScan.Error);Assert.IsNull(npcError);Assert.IsNull(game.Error);
                Assert.IsTrue(game.Popup.gameObject.activeSelf);Assert.AreEqual(ball+1,requests);Assert.AreEqual(ball,reels.CoinScan.Rewards.Count);
                Assert.IsTrue(reels.BallScan.IsRunning);Assert.AreEqual(0,done);
                for(int i=0;i<50&&!game.Popup.PlainButton.gameObject.activeInHierarchy;i++)yield return null;
                Assert.IsTrue(game.Popup.PlainButton.gameObject.activeInHierarchy);
                float reward=game.Popup.Claim.OriginalReward*.5f;game.Popup.PlainButton.onClick.Invoke();
                deadline=Time.realtimeSinceStartup+5;
                while(reels.CoinScan.Rewards.Count==ball&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsNull(game.Error);Assert.AreEqual(ball+1,reels.CoinScan.Rewards.Count);
                Assert.AreEqual(reward,reels.CoinScan.Rewards[expected[ball].gameObject]);paid+=reward;
                Assert.AreEqual(initialBalance+paid,entry.PlayerProgress.GreenCount);
                Assert.AreEqual(initialTotal+paid,entry.PlayerProgress.TotalFreeSpinWin);
                Assert.IsFalse(game.IsRunning);Assert.IsFalse(game.Popup.gameObject.activeSelf);
                Assert.IsTrue(reels.Specials.CurrentStoppedBall(expected[ball]).RewardPresentation.IsAnimating);
            }
            for(int i=0;i<25&&done==0;i++)yield return null;
            Assert.AreEqual(1,done);Assert.IsFalse(reels.BallScan.IsRunning);Assert.IsNull(reels.BallScan.Error);
            Assert.AreEqual(RecoveredSlotType.Free,entry.PlayerProgress.GameSlotType);
        } finally {
            if(reels!=null)Object.Destroy(reels.gameObject);if(npc!=null)Object.Destroy(npc.gameObject);if(game!=null)Object.Destroy(game.gameObject);if(target!=null)Object.Destroy(target);
            if(scene.IsValid())unload=SceneManager.UnloadSceneAsync(scene);Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
