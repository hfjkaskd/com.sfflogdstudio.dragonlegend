using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredBonusRewardClaimTests
{
    private sealed class View:IRecoveredBonusRewardClaimView
    {
        public readonly List<string> Order=new List<string>();
        public Action CountCompleted,FlightCompleted;
        public float From,To,FlightAmount;
        public void PlaySound(string name)=>Order.Add(name);
        public void CountReward(float from,float to,Action completed){From=from;To=to;CountCompleted=completed;Order.Add("counting");}
        public void HideWheelIfShown()=>Order.Add("wheel");
        public void HideReward()=>Order.Add("hide");
        public void FlyCoin(float amount,Action completed){FlightAmount=amount;FlightCompleted=completed;Order.Add("flight");}
    }
    [TestCase(AdOutcome.Cancelled)]
    [TestCase(AdOutcome.Unavailable)]
    [TestCase(AdOutcome.Failed)]
    public void AdFailureRetainsAmountAndUnlocksRetry(AdOutcome outcome)
    {
        var view=new View();var ads=new LocalAdFacade();var claim=new RecoveredBonusRewardClaim(ads,view,2,.5f);
        claim.BeforeShow(963,_=>Assert.Fail("Early callback"));claim.OnClickButton("ClaimBtn");
        Assert.IsTrue(ads.Pending);Assert.AreEqual("lucky",ads.Placement);Assert.AreEqual("lucky",ads.Scene);
        claim.OnClickButton("UnPlayBtn");Assert.AreEqual(0,ads.InterstitialCount);
        ads.Complete(outcome);Assert.IsFalse(claim.IsClicked);Assert.AreEqual(963,claim.Reward);
        CollectionAssert.AreEqual(new[]{"click"},view.Order);
        claim.OnClickButton("ClaimBtn");ads.Complete(AdOutcome.Rewarded);
        Assert.AreEqual(1926,claim.Reward);Assert.AreEqual(963,view.From);Assert.AreEqual(1926,view.To);
        Assert.AreEqual(963,claim.OriginalReward);Assert.IsTrue(claim.IsClicked);
        CollectionAssert.AreEqual(new[]{"click","click","count","counting"},view.Order);
    }
    [Test]
    public void OrdinaryClaimKeepsFractionAndReleasesCallerOnlyAtFlightArrivalBeforeCredit()
    {
        var view=new View();var ads=new LocalAdFacade();var claim=new RecoveredBonusRewardClaim(ads,view,2,.5f);
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig());
        var progress=new RecoveredPlayerProgress(rules,()=>view.Order.Add("save"),new PlayerData{GreenCount=10});
        var rewards=new RecoveredRewardBranches(rules,progress);float received=-1;
        claim.BeforeShow(963,value=>{received=value;view.Order.Add("caller");});
        claim.OnClickButton("UnPlayBtn");
        Assert.AreEqual("iv_close",ads.Placement);Assert.AreEqual("lucky",ads.Scene);Assert.AreEqual(1,ads.InterstitialCount);
        Assert.AreEqual(481.5f,claim.Reward);Assert.AreEqual(-1,received);Assert.IsNull(view.FlightCompleted);
        view.CountCompleted();CollectionAssert.AreEqual(new[]{"click","count","counting","wheel","hide"},view.Order);
        claim.AfterHide();Assert.AreEqual(-1,received);Assert.AreEqual(10,progress.GreenCount);
        rewards.CompleteFlyCoin(view.FlightAmount,view.FlightCompleted,()=>view.Order.Add("title"));
        Assert.AreEqual(481.5f,received);Assert.AreEqual(491.5f,progress.GreenCount);
        CollectionAssert.AreEqual(new[]{"click","count","counting","wheel","hide","flight","caller","title","save","save"},view.Order);
    }
    [Test]
    public void ZeroStillRequestsAdThenSkipsCountAndNewShowResetsUnknownButtonLatch()
    {
        var view=new View();var ads=new LocalAdFacade();var claim=new RecoveredBonusRewardClaim(ads,view,2,.5f);
        claim.BeforeShow(5,null);claim.OnClickButton("Unknown");claim.OnClickButton("ClaimBtn");
        Assert.IsTrue(claim.IsClicked);Assert.IsFalse(ads.Pending);Assert.IsEmpty(view.Order);
        claim.BeforeShow(0,null);Assert.IsFalse(claim.IsClicked);claim.OnClickButton("ClaimBtn");
        Assert.IsTrue(ads.Pending);ads.Complete(AdOutcome.Rewarded);Assert.IsNull(view.CountCompleted);
        CollectionAssert.AreEqual(new[]{"click","wheel","hide"},view.Order);
        claim.AfterHide();Assert.AreEqual(0,view.FlightAmount);view.FlightCompleted();
    }
}
