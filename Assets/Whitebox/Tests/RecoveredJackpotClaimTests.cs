using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredJackpotClaimTests
{
    private sealed class View : IRecoveredJackpotClaimView
    {
        public readonly List<string> Order = new List<string>();
        public Action CountCompleted, FlightCompleted;
        public float From, To, FlightAmount;
        public void PlaySound(string name) => Order.Add(name);
        public void CountReward(float from,float to,Action completed)
        { From=from;To=to;CountCompleted=completed;Order.Add("tween"); }
        public void HideWheelIfShown() => Order.Add("wheel");
        public void HideJackpot() => Order.Add("hide");
        public void StopSound1() => Order.Add("stop");
        public void ResumeMusic() => Order.Add("resume");
        public void FlyCoin(float amount,Action completed)
        { FlightAmount=amount;FlightCompleted=completed;Order.Add("fly"); }
    }

    private static RecoveredGameplayRules Rules(int advertised=2000,int plain=500) =>
        new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
            Qonrii=new QonriiPoro {JpQloim=new List<int>{advertised,plain}}});

    [TestCase(AdOutcome.Cancelled)]
    [TestCase(AdOutcome.Unavailable)]
    [TestCase(AdOutcome.Failed)]
    public void FailedAdUnlocksRetryWithoutCountingHidingOrCrediting(AdOutcome outcome)
    {
        var rules=Rules();int saves=0,calls=0;
        var progress=new RecoveredPlayerProgress(rules,()=>saves++,new PlayerData{GreenCount=10});
        var view=new View();var ads=new LocalAdFacade();var claim=new RecoveredJackpotClaim(progress,rules,ads,view);
        claim.BeforeShow(8,_=>calls++);
        claim.OnClickButton("ClaimBtn");
        Assert.IsTrue(ads.Pending);Assert.IsTrue(claim.IsClicked);
        Assert.AreEqual("jackpot",ads.Placement);Assert.AreEqual("jackpot",ads.Scene);
        claim.OnClickButton("UnPlayBtn");claim.OnClickButton("ClaimBtn");
        Assert.AreEqual(0,ads.InterstitialCount);
        ads.Complete(outcome);
        Assert.IsFalse(claim.IsClicked);Assert.AreEqual(8,claim.Reward);
        CollectionAssert.AreEqual(new[]{"click"},view.Order);
        Assert.AreEqual(10,progress.GreenCount);Assert.AreEqual(0,saves);Assert.AreEqual(0,calls);
        claim.OnClickButton("ClaimBtn");ads.Complete(AdOutcome.Rewarded);
        Assert.AreEqual(16,claim.Reward);Assert.AreEqual(8,view.From);Assert.AreEqual(16,view.To);
        Assert.AreEqual(8,claim.OriginalReward);Assert.IsTrue(claim.IsClicked);
        CollectionAssert.AreEqual(new[]{"click","click","count","tween"},view.Order);
    }

    [Test]
    public void CountThenWindowExitThenFlightPreserveNativeCallbackAndCreditOrder()
    {
        var rules=Rules();var view=new View();
        var progress=new RecoveredPlayerProgress(rules,()=>view.Order.Add("save"),new PlayerData{GreenCount=10});
        var ads=new LocalAdFacade();var claim=new RecoveredJackpotClaim(progress,rules,ads,view);
        float received=-1;
        claim.BeforeShow(8,amount=>{received=amount;view.Order.Add("caller");});
        claim.OnClickButton("UnPlayBtn");
        Assert.AreEqual("iv_close",ads.Placement);Assert.AreEqual("jackpot",ads.Scene);
        Assert.AreEqual(1,ads.InterstitialCount);Assert.IsFalse(ads.Pending);
        Assert.AreEqual(4,claim.Reward);Assert.IsNull(view.FlightCompleted);
        Assert.AreEqual(-1,received);
        view.CountCompleted();
        CollectionAssert.AreEqual(new[]{"click","count","tween","wheel","hide"},view.Order);
        Assert.AreEqual(-1,received);Assert.AreEqual(10,progress.GreenCount);
        claim.AfterHide();
        Assert.AreEqual(4,view.FlightAmount);Assert.AreEqual(-1,received);
        var branches=new RecoveredRewardBranches(rules,progress);
        branches.CompleteFlyCoin(view.FlightAmount,view.FlightCompleted,()=>view.Order.Add("title"));
        Assert.AreEqual(4,received);Assert.AreEqual(14,progress.GreenCount);
        CollectionAssert.AreEqual(new[]{"click","count","tween","wheel","hide","stop","resume","fly","caller","title","save","save"},view.Order);
    }

    [Test]
    public void FirstFreeSkipsAdAndCountButDoesNotConsumeFlagOrCompleteOnHide()
    {
        var rules=Rules();var progress=new RecoveredPlayerProgress(rules,()=>Assert.Fail("Unexpected save"),new PlayerData());
        progress.IsFirstFreeReward=true;
        var ads=new LocalAdFacade();var view=new View();var claim=new RecoveredJackpotClaim(progress,rules,ads,view);
        int calls=0;claim.BeforeShow(8,_=>calls++);
        Assert.IsTrue(claim.FirstFreeAtShow);Assert.AreEqual(1,claim.AdvertisedMultiplier);
        Assert.AreEqual(.5f,claim.UnadvertisedMultiplier);
        claim.OnClickButton("ClaimBtn");
        Assert.IsFalse(ads.Pending);Assert.AreEqual(8,claim.Reward);Assert.IsTrue(progress.IsFirstFreeReward);
        CollectionAssert.AreEqual(new[]{"click","wheel","hide"},view.Order);
        claim.AfterHide();Assert.AreEqual(0,calls);view.FlightCompleted();Assert.AreEqual(1,calls);
    }

    [TestCase(false,true,16f,false)]
    [TestCase(true,false,8f,true)]
    public void ClickRereadsFirstFreeFlagButUsesMultiplierCapturedAtShow(bool atShow,bool atClick,float reward,bool pending)
    {
        var rules=Rules();var progress=new RecoveredPlayerProgress(rules,()=>{},new PlayerData());
        var ads=new LocalAdFacade();var view=new View();var claim=new RecoveredJackpotClaim(progress,rules,ads,view);
        progress.IsFirstFreeReward=atShow;claim.BeforeShow(8,null);progress.IsFirstFreeReward=atClick;
        claim.OnClickButton("ClaimBtn");Assert.AreEqual(pending,ads.Pending);
        if(pending)ads.Complete(AdOutcome.Rewarded);
        Assert.AreEqual(reward,claim.Reward);
    }

    [Test]
    public void EqualAmountSkipsTweenAndNewShowResetsUnknownButtonLatch()
    {
        var rules=Rules(1000,1000);var progress=new RecoveredPlayerProgress(rules,()=>{},new PlayerData());
        var ads=new LocalAdFacade();var view=new View();var claim=new RecoveredJackpotClaim(progress,rules,ads,view);
        claim.BeforeShow(8,null);claim.OnClickButton("Unknown");claim.OnClickButton("ClaimBtn");
        Assert.IsTrue(claim.IsClicked);Assert.IsFalse(ads.Pending);Assert.IsEmpty(view.Order);
        claim.BeforeShow(12,null);Assert.IsFalse(claim.IsClicked);
        claim.OnClickButton("ClaimBtn");ads.Complete(AdOutcome.Rewarded);
        Assert.AreEqual(12,claim.Reward);Assert.IsNull(view.CountCompleted);
        CollectionAssert.AreEqual(new[]{"click","wheel","hide"},view.Order);
    }
}
