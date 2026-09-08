using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredBigWinClaimTests
{
    private sealed class View : IRecoveredBigWinClaimView
    {
        public readonly List<string> Order=new List<string>();
        public Action CountCompleted;
        public float From,To,FlightAmount;
        public void PlaySound(string name)=>Order.Add(name);
        public void CountReward(float from,float to,Action completed)
        {From=from;To=to;CountCompleted=completed;Order.Add("counting");}
        public void HideBigWin()=>Order.Add("hide");
        public void StopSound1()=>Order.Add("stop");
        public void ResumeMusic()=>Order.Add("resume");
        public void FlyCoin(float amount){FlightAmount=amount;Order.Add("fly");}
    }
    private static RecoveredGameplayRules Rules(int ad=2500,int plain=500)=>
        new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{
            Qonrii=new QonriiPoro{RiikinQloim=new List<int>{ad,plain},JpQloim=new List<int>{9000,9000}}});

    [TestCase(AdOutcome.Cancelled)]
    [TestCase(AdOutcome.Failed)]
    [TestCase(AdOutcome.Unavailable)]
    public void AdFailureAllowsRetryAndSuccessCountsFromOriginalBeforeHide(AdOutcome outcome)
    {
        var rules=Rules();var view=new View();var ads=new LocalAdFacade();int saves=0,calls=0;
        var progress=new RecoveredPlayerProgress(rules,()=>saves++,new PlayerData{GreenCount=10});
        var claim=new RecoveredBigWinClaim(progress,rules,ads,view);
        claim.BeforeShow(8,_=>calls++);Assert.AreEqual(0,claim.Reward);
        claim.OnClickButton("ClaimBtn");Assert.IsTrue(ads.Pending);
        Assert.AreEqual("bigwin",ads.Placement);Assert.AreEqual("bigwin",ads.Scene);
        claim.OnClickButton("UnPlayBtn");Assert.AreEqual(0,ads.InterstitialCount);
        ads.Complete(outcome);Assert.IsFalse(claim.IsClicked);Assert.AreEqual(0,claim.Reward);
        Assert.IsNull(view.CountCompleted);Assert.AreEqual(0,calls);
        claim.OnClickButton("ClaimBtn");ads.Complete(AdOutcome.Rewarded);
        Assert.AreEqual(20,claim.Reward);Assert.AreEqual(8,claim.OriginalReward);
        Assert.AreEqual(8,view.From);Assert.AreEqual(20,view.To);
        CollectionAssert.AreEqual(new[]{"click","click","count","counting"},view.Order);
        view.CountCompleted();Assert.AreEqual("hide",view.Order[4]);
        Assert.AreEqual(0,calls);Assert.AreEqual(0,saves);Assert.AreEqual(10,progress.GreenCount);
    }
    [Test]
    public void MainFlowCallbackPrecedesCashFlightAndNeitherClaimsCredit()
    {
        var rules=Rules();var view=new View();var ads=new LocalAdFacade();int saves=0;
        var progress=new RecoveredPlayerProgress(rules,()=>saves++,new PlayerData{GreenCount=10});
        var claim=new RecoveredBigWinClaim(progress,rules,ads,view);
        claim.BeforeShow(8,amount=>{Assert.AreEqual(4,amount);view.Order.Add("caller");});
        claim.OnClickButton("UnPlayBtn");Assert.AreEqual("iv_close",ads.Placement);
        Assert.AreEqual("bigwin",ads.Scene);Assert.AreEqual(1,ads.InterstitialCount);
        view.CountCompleted();claim.AfterHide();
        CollectionAssert.AreEqual(new[]{"click","count","counting","hide","stop","resume","caller","fly"},view.Order);
        Assert.AreEqual(4,view.FlightAmount);Assert.AreEqual(10,progress.GreenCount);Assert.AreEqual(0,saves);
    }
    [Test]
    public void LiveFirstFreeFlagDoesNotReplaceShowTimeMultiplier()
    {
        var rules=Rules();var view=new View();var ads=new LocalAdFacade();
        var progress=new RecoveredPlayerProgress(rules,()=>{},new PlayerData());
        var claim=new RecoveredBigWinClaim(progress,rules,ads,view);
        claim.BeforeShow(8,null);progress.IsFirstFreeReward=true;claim.OnClickButton("ClaimBtn");
        Assert.IsFalse(ads.Pending);Assert.AreEqual(20,claim.Reward);
        claim.BeforeShow(8,null);Assert.IsTrue(claim.FirstFreeAtShow);
        progress.IsFirstFreeReward=false;claim.OnClickButton("ClaimBtn");Assert.IsTrue(ads.Pending);
        view.Order.Clear();ads.Complete(AdOutcome.Rewarded);
        Assert.AreEqual(8,claim.Reward);CollectionAssert.AreEqual(new[]{"hide"},view.Order);
    }
    [Test]
    public void UnknownButtonLatchesAndReopenResetsUnclaimedReward()
    {
        var rules=Rules();var view=new View();var ads=new LocalAdFacade();
        var progress=new RecoveredPlayerProgress(rules,()=>{},new PlayerData());
        var claim=new RecoveredBigWinClaim(progress,rules,ads,view);
        claim.BeforeShow(8,null);claim.OnClickButton("Close");claim.OnClickButton("ClaimBtn");
        Assert.IsTrue(claim.IsClicked);Assert.IsFalse(ads.Pending);Assert.IsEmpty(view.Order);
        claim.BeforeShow(12,null);Assert.IsFalse(claim.IsClicked);Assert.AreEqual(0,claim.Reward);
        Assert.AreEqual(12,claim.OriginalReward);
    }
    [TestCase(-500,-.5f)]
    [TestCase(0,0)]
    [TestCase(999,.999f)]
    public void BigWinClaimUsesItsOwnConfigAndPreservesFractionalAndNegativeValues(int value,float expected)
    {Assert.AreEqual(expected,Rules(value).GetBigWinClaim(0));}
}
