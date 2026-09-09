using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredTreasureClaimTests
{
    private sealed class View:IRecoveredTreasureClaimView
    {
        public readonly List<string> Order=new List<string>();
        public Func<float> ReadOriginal;
        public float Target,FlightAmount;
        public Action CountCompleted,FlightCompleted;
        public void PlaySound(string name)=>Order.Add(name);
        public void CountReward(Func<float> original,float to,Action completed)
        {ReadOriginal=original;Target=to;CountCompleted=completed;Order.Add("counting");}
        public void HideTreasure()=>Order.Add("hide");
        public void FlyCoin(float amount,Action completed){FlightAmount=amount;FlightCompleted=completed;Order.Add("cash-flight");}
        public void FlyCollectCard()=>Order.Add("card-flight");
    }
    private static RecoveredGameplayRules Rules(int advertised=2000,int plain=500)=>new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{
        Qollgqr=new QollgqrPoro{QollgqrQloim=new List<int>{advertised,plain}}});

    [TestCase(AdOutcome.Cancelled)]
    [TestCase(AdOutcome.Unavailable)]
    [TestCase(AdOutcome.Failed)]
    public void FailedAdUnlocksRetryAndSuccessReadsLiveCardWorth(AdOutcome outcome)
    {
        var view=new View();var ads=new LocalAdFacade();var claim=new RecoveredTreasureClaim(ads,view);
        var info=new RecoveredCollectInfo{id=7,worth=963};claim.BeforeShow(info,Rules(),null);
        Assert.AreEqual(0,claim.Reward);claim.OnClickButton("ClaimBtn");
        Assert.AreEqual("treasure",ads.Placement);Assert.AreEqual("treasure",ads.Scene);
        claim.OnClickButton("UnPlayBtn");Assert.AreEqual(0,ads.InterstitialCount);
        ads.Complete(outcome);Assert.IsFalse(claim.IsClicked);Assert.AreEqual(0,claim.Reward);
        info.worth=1001;claim.OnClickButton("ClaimBtn");ads.Complete(AdOutcome.Rewarded);
        Assert.AreEqual(2002,claim.Reward);Assert.AreEqual(2002,view.Target);Assert.AreEqual(1001,view.ReadOriginal());
        info.worth=1234;Assert.AreEqual(1234,view.ReadOriginal(),"Original tween getter reads at animation startup.");
        Assert.AreEqual(2002,claim.Reward);Assert.IsTrue(claim.IsClicked);
        CollectionAssert.AreEqual(new[]{"click","click","count","counting"},view.Order);
    }
    [Test]
    public void PlainClaimHidesThenStartsCashAndCardFlightsAndCreditsAfterCaller()
    {
        var view=new View();var ads=new LocalAdFacade();var claim=new RecoveredTreasureClaim(ads,view);
        var rules=Rules();var data=new PlayerData{GreenCount=10};
        var player=new RecoveredPlayerProgress(rules,()=>view.Order.Add("save"),data);
        var branches=new RecoveredRewardBranches(rules,player);float received=-1;
        claim.BeforeShow(new RecoveredCollectInfo{id=3,worth=963},rules,value=>{
            Assert.AreEqual(10,player.GreenCount);received=value;view.Order.Add("caller");});
        claim.OnClickButton("UnPlayBtn");
        Assert.AreEqual("iv_close",ads.Placement);Assert.AreEqual("treasure",ads.Scene);Assert.AreEqual(1,ads.InterstitialCount);
        Assert.AreEqual(481.5f,claim.Reward);Assert.AreEqual(-1,received);Assert.IsNull(view.FlightCompleted);
        view.CountCompleted();claim.AfterHide();Assert.AreEqual(-1,received);
        CollectionAssert.AreEqual(new[]{"click","count","counting","hide","cash-flight","card-flight"},view.Order);
        branches.CompleteFlyCoin(view.FlightAmount,view.FlightCompleted,()=>view.Order.Add("title"));
        Assert.AreEqual(481.5f,received);Assert.AreEqual(491.5f,player.GreenCount);
        CollectionAssert.AreEqual(new[]{"click","count","counting","hide","cash-flight","card-flight","caller","title","save","save"},view.Order);
        Assert.IsEmpty(data.PlayerCollectDatas,"Treasure entry records the card; claiming does not mutate collection records.");
    }
    [Test]
    public void UnchangedRewardSkipsCounterAndNextShowPreservesStoredReward()
    {
        var view=new View();var ads=new LocalAdFacade();var claim=new RecoveredTreasureClaim(ads,view);
        claim.BeforeShow(new RecoveredCollectInfo{worth=17},Rules(1000,1000),null);
        claim.OnClickButton("UnPlayBtn");Assert.AreEqual(17,claim.Reward);Assert.IsNull(view.ReadOriginal);
        CollectionAssert.AreEqual(new[]{"click","hide"},view.Order);
        claim.BeforeShow(new RecoveredCollectInfo{worth=999},Rules(),null);
        Assert.IsFalse(claim.IsClicked);Assert.AreEqual(17,claim.Reward);
        claim.OnClickButton("Unknown");claim.OnClickButton("ClaimBtn");
        Assert.IsTrue(claim.IsClicked);Assert.IsFalse(ads.Pending);
        Assert.AreEqual(1,ads.InterstitialCount);
    }
    [Test]
    public void ZeroWorthStillRequestsAdButHidesWithoutCountOnSuccess()
    {
        var view=new View();var ads=new LocalAdFacade();var claim=new RecoveredTreasureClaim(ads,view);
        claim.BeforeShow(new RecoveredCollectInfo{worth=0},Rules(),null);claim.OnClickButton("ClaimBtn");
        Assert.IsTrue(ads.Pending);ads.Complete(AdOutcome.Rewarded);
        Assert.IsNull(view.ReadOriginal);CollectionAssert.AreEqual(new[]{"click","hide"},view.Order);
        claim.AfterHide();Assert.AreEqual(0,view.FlightAmount);view.FlightCompleted();
    }
}
