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

public sealed class RecoveredFreeBonusFlowTests
{
    [UnityTest]
    public IEnumerator FreeScanEntersActualBonusWindowAndReturnsToFreeMusic()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=UnityEngine.Random.state;
        Scene scene=default;AsyncOperation unload=null;RecoveredFreeReels reels=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.025f;UnityEngine.Random.InitState(61);
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry entry=null;
            foreach(var root in scene.GetRootGameObjects()){var value=root.GetComponentInChildren<GameEntry>();if(value!=null)entry=value;}
            Assert.IsNotNull(entry);{ float resourceDeadline=Time.realtimeSinceStartup+5; while(entry.BonusFlow==null&&Time.realtimeSinceStartup<resourceDeadline)yield return null; }
            var flow=entry.BonusFlow;Assert.IsNotNull(flow);
            var weights=new List<int>();for(int i=0;i<=15;i++)weights.Add(i==15?1000000:0);
            var generationRules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=new RrggiomgPoro{
                QoinOmoinrKgiitr=weights,RollOmoinrKgiitr=new List<int>{1000000},RollRipgKgiitr=new List<int>{1}}});
            var result=new RecoveredFreeSpinResult(generationRules);result.Begin(new[]{3});while(result.IsGenerating)result.Step();
            reels=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
            reels.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),result);
            entry.PlayerProgress.GameSlotType=RecoveredSlotType.Free;
            reels.CoinScan.Bind(entry.Rules,entry.PlayerProgress,result,entry.Playfield.BonusCollection,()=>0);
            flow.BindFreeScan(reels.CoinScan);
            Exception failure=null;flow.Failed+=error=>failure=error;
            int completed=0;var music=new List<string>();flow.Completed+=()=>completed++;flow.ChangeMusicRequested+=music.Add;
            reels.Controller.Begin();
            for(int i=0;i<1200&&!flow.Window.gameObject.activeSelf;i++)yield return null;
            Assert.IsNull(reels.CoinScan.Error);Assert.IsNull(failure);Assert.IsTrue(flow.IsRunning);
            Assert.IsTrue(flow.Window.gameObject.activeSelf);Assert.AreEqual(15,reels.CoinScan.Rewards.Count);
            CollectionAssert.AreEqual(new[]{0,0,0,0,0},entry.PlayerProgress.BonusArea);
            Assert.AreEqual(RecoveredSlotType.Free,entry.PlayerProgress.GameSlotType);Assert.AreEqual(0,completed);
            for(int i=0;i<150&&!music.Contains("bonusBg");i++)yield return null;
            Assert.Contains("bonusBg",music);
            var reward=flow.Window.GetComponentInChildren<RecoveredBonusRewardPopup>(true);
            var jackpot=flow.Window.GetComponentInChildren<RecoveredJackpotPopup>(true);
            for(int card=0;card<entry.Rules.GetBonusFreeTimes();card++) {
                flow.Window.Card(card).Button.onClick.Invoke();float deadline=Time.realtimeSinceStartup+8;
                while(flow.Window.Selection.IsClicked&&Time.realtimeSinceStartup<deadline) {
                    if(reward.gameObject.activeInHierarchy)reward.PlainButton.onClick.Invoke();
                    if(jackpot.gameObject.activeInHierarchy)jackpot.PlainButton.onClick.Invoke();
                    yield return null;
                }
                Assert.IsNull(failure);Assert.IsFalse(flow.Window.Selection.IsClicked);
            }
            Assert.IsTrue(flow.Window.CloseButton.gameObject.activeInHierarchy);flow.Window.CloseButton.onClick.Invoke();
            for(int i=0;i<240&&completed==0;i++)yield return null;
            Assert.IsNull(failure);Assert.AreEqual(1,completed);Assert.IsFalse(flow.IsRunning);
            Assert.IsFalse(flow.Window.gameObject.activeSelf);CollectionAssert.AreEqual(new[]{"bonusBg","freeBg"},music);
            // Exercise explicit rebinding and detach before scene cleanup.
            flow.BindFreeScan(reels.CoinScan);flow.BindFreeScan(null);
            Assert.AreEqual(RecoveredSlotType.Free,entry.PlayerProgress.GameSlotType);
        } finally {
            if(reels!=null)Object.Destroy(reels.gameObject);
            if(scene.IsValid())unload=SceneManager.UnloadSceneAsync(scene);
            Time.timeScale=scale;Time.captureDeltaTime=delta;UnityEngine.Random.state=random;
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
