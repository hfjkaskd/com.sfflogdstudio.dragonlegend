using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredFreeCoinScanTests
{
    private static RecoveredFreeReels Create(out RecoveredGameplayRules rules,out RecoveredFreeSpinResult result)
    {
        var weights=new List<int>();for(int i=0;i<=15;i++)weights.Add(i==15?1000000:0);
        rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
            Ronig=new RonigPoro{QoinRgkorp=new List<int>{4,4}},
            Rrggiomg=new RrggiomgPoro{QoinOmoinrKgiitr=weights,RollOmoinrKgiitr=new List<int>{1000000},RollRipgKgiitr=new List<int>{1}}
        });
        result=new RecoveredFreeSpinResult(rules);result.Begin(new[]{3});while(result.IsGenerating)result.Step();
        var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        root.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),result);return root;
    }
    [UnityTest]
    public IEnumerator ActualStopsAutomaticallyScanAndAccumulateWithoutSavingOrAwaitingLastPresentation()
    {
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var root=Create(out var rules,out var result);
        var collection=Object.Instantiate(Resources.Load<RecoveredBonusCollection>("RecoveredUI/BonusCollection"));
        var data=new PlayerData{BonusArea=new List<int>{0,0,0,0,0},GreenCount=12};int saves=0,done=0,reveals=0;
        var progress=new RecoveredPlayerProgress(rules,()=>saves++,data){TotalFreeSpinWin=10};
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;collection.Initialize(new[]{0,0,0,0,0});
            root.CoinScan.Bind(rules,progress,result,collection,()=>0);
            root.CoinScan.Completed+=()=>done++;
            root.Specials.SoundRequested+=sound=> {
                if(sound!="coinReveal")return;
                int index=reveals%15;
                Assert.AreEqual(index,root.CoinScan.Rewards.Count,"Reveal precedes dictionary Add.");
                Assert.AreEqual(10+reveals*4,progress.TotalFreeSpinWin,"Reveal precedes total mutation.");
                Assert.AreEqual((reveals/15)*3+index%3+1,data.BonusArea[index/3]);
                reveals++;
            };
            for(int pass=0;pass<2;pass++) {
                root.Controller.Begin();Assert.AreEqual(0,root.CoinScan.Rewards.Count);
                for(int i=0;i<400&&done<=pass;i++)yield return null;
                Assert.IsNull(root.Controller.Error);Assert.IsNull(root.CoinScan.Error);
                Assert.AreEqual(pass+1,done);Assert.AreEqual(15,root.CoinScan.Rewards.Count);
                Assert.AreEqual(10+(pass+1)*60,progress.TotalFreeSpinWin);
                for(int column=0;column<5;column++)for(int row=0;row<3;row++)
                    Assert.AreEqual(4,root.CoinScan.Rewards[root.At(column,row).gameObject]);
                Assert.IsTrue(root.Specials.CurrentStoppedCoin(root.At(4,2)).RewardPresentation.IsPresenting);
                // Finish the independently running reveal/flight before the next spin.
                for(int i=0;i<15;i++)yield return null;
            }
            Assert.AreEqual(30,reveals);Assert.AreEqual(0,saves);Assert.AreEqual(12,data.GreenCount);
        } finally {Object.Destroy(root.gameObject);Object.Destroy(collection.gameObject);Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }
    [UnityTest]
    public IEnumerator InitialPoolCoinsDoNotCountAsStoppedRewardCallbacks()
    {
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var root=Create(out var rules,out var result);
        var collection=Object.Instantiate(Resources.Load<RecoveredBonusCollection>("RecoveredUI/BonusCollection"));
        var data=new PlayerData{BonusArea=new List<int>{0,0,0,0,0}};int saves=0,done=0;
        var progress=new RecoveredPlayerProgress(rules,()=>saves++,data){TotalFreeSpinWin=10};
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            root.CoinScan.Bind(rules,progress,result,collection,()=>0);root.CoinScan.Completed+=()=>done++;
            root.CoinScan.Begin();Assert.AreEqual(1,data.BonusArea[0]);
            Time.timeScale=0;for(int i=0;i<5;i++)yield return null;Assert.AreEqual(1,data.BonusArea[0]);
            Time.timeScale=1;for(int i=0;i<250&&done==0;i++)yield return null;
            Assert.IsNull(root.CoinScan.Error);Assert.AreEqual(1,done);Assert.AreEqual(0,root.CoinScan.Rewards.Count);
            CollectionAssert.AreEqual(new[]{3,3,3,3,3},data.BonusArea);
            Assert.AreEqual(10,progress.TotalFreeSpinWin);Assert.AreEqual(0,saves);
        } finally {Object.Destroy(root.gameObject);Object.Destroy(collection.gameObject);Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }
}
