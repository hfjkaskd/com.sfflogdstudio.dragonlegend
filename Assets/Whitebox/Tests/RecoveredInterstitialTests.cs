using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredInterstitialTests
{
    private static RecoveredGameplayRules Rules(int chance=999)=>new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{
        Qonrii=new QonriiPoro{Lgtgl=new List<int>{1,4},InggrrRonpom=new List<int>{chance,chance,chance},InggrrQP=new List<int>{60}}});
    [Test]
    public void SuccessfulCallbackStartsSharedCooldownAndFailureDoesNot()
    {
        var prior=Random.state;
        try
        {
            long now=1000;bool isA=true;int level=2;
            int taskCount=0;
            var policy=new RecoveredInterstitialPolicy(Rules(),()=>isA,()=>level,()=>now,()=>taskCount++);var ads=new LocalAdFacade(policy);
            ads.PlayInterAd("iv_close","bank");Assert.AreEqual(0,ads.InterstitialCount);Assert.AreEqual(0,policy.LastPlayTime);
            isA=false;level=1;ads.PlayInterAd("iv_close","bank");Assert.AreEqual(0,ads.InterstitialCount);
            level=2;ads.PlayInterAd("iv_close","bank");Assert.AreEqual(1,ads.InterstitialCount);Assert.IsTrue(ads.InterstitialPending);Assert.IsFalse(ads.Pending);
            Assert.AreEqual(940,policy.LastPlayTime);ads.CompleteInterstitial(false);Assert.AreEqual(940,policy.LastPlayTime);Assert.AreEqual(0,taskCount);
            ads.PlayInterAd("iv_close","treasure");Assert.AreEqual(2,ads.InterstitialCount);
            now=1005;ads.CompleteInterstitial(true);Assert.AreEqual(1005,policy.LastPlayTime);Assert.AreEqual(1,taskCount);
            now=1064;ads.PlayInterAd("iv_close","jackpot");Assert.AreEqual(2,ads.InterstitialCount);
            now=1065;ads.PlayInterAd("iv_close","jackpot");Assert.AreEqual(3,ads.InterstitialCount);
            Assert.AreEqual("jackpot",ads.Scene);ads.CompleteInterstitial(true);Assert.IsFalse(ads.CompleteInterstitial(true));
        }
        finally{Random.state=prior;}
    }
    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(20)]
    public void ProbabilityMatchesNativeInclusiveRollAndMinimumConsumesNoRandom(int level)
    {
        var prior=Random.state;
        try
        {
            var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Qonrii=new QonriiPoro{
                Lgtgl=new List<int>{1,4},InggrrRonpom=new List<int>{0,500,999},InggrrQP=new List<int>{60}}});
            for(int seed=0;seed<32;seed++)
            {
                int chance=level>=4?999:level==3?500:0;
                Random.InitState(seed);bool expected=level>1&&Random.Range(0,1000)<=chance;int next=Random.Range(0,100000);
                Random.InitState(seed);Assert.AreEqual(expected,rules.CheckInsertAd(level));Assert.AreEqual(next,Random.Range(0,100000));
            }
        }
        finally{Random.state=prior;}
    }
    [UnityTest]
    public IEnumerator ActualPresenterCompletesInterstitialWithoutRewardCallback()
    {
        var host=new GameObject("Interstitial test",typeof(RectTransform),typeof(Canvas));
        var template=Resources.Load<GameObject>("Whitebox/GameEntry").GetComponentInChildren<RecoveredAdSimulationControls>(true);
        var view=Object.Instantiate(template,host.transform,false);
        try
        {
            var rules=Rules();var data=new PlayerData();int saves=0;
            data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,step=1,count=3});
            data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=1,step=2,count=8});
            var player=new RecoveredPlayerProgress(rules,()=>saves++,data);
            var ads=new LocalAdFacade(new RecoveredInterstitialPolicy(rules,()=>false,()=>2,()=>1000,()=>player.RefreshCashOutTask(1,1)));
            view.Bind(ads);int rewardEvents=0;ads.RewardAdCompleted+=outcome=>rewardEvents++;
            ads.PlayInterAd("iv_close","bigwin");Assert.IsTrue(ads.InterstitialPending);Assert.IsFalse(ads.Pending);
            var tip=view.GetComponentInChildren<RecoveredTipsWindow>(true);Assert.AreEqual("Simulated interstitial started",tip.Label.text);
            yield return new WaitForSecondsRealtime(2.2f);
            Assert.IsFalse(ads.InterstitialPending);Assert.AreEqual(0,rewardEvents);Assert.AreEqual("Simulated interstitial completed",tip.Label.text);
            Assert.AreEqual(4,data.PlayerCashOutDatas[0].count);Assert.AreEqual(8,data.PlayerCashOutDatas[1].count);Assert.AreEqual(1,saves);
        }
        finally{Object.Destroy(host);}
    }
}
