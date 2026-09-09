using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredMoreSpinClaimTests
{
    private sealed class View:IRecoveredMoreSpinView
    {
        public int Hides,Tips;public readonly List<string> Sounds=new List<string>();
        public void PlaySound(string sound)=>Sounds.Add(sound);
        public void ShowLimitTip()=>Tips++;
        public void Hide()=>Hides++;
    }
    [TestCase(AdOutcome.Cancelled)]
    [TestCase(AdOutcome.Unavailable)]
    [TestCase(AdOutcome.Failed)]
    public void FailureAllowsRetryThenSuccessReadsLiveCountAndGrant(AdOutcome failure)
    {
        var config=new GoldenDragonAutoGenConfig{Qonrii=new QonriiPoro{MojGping=new List<int>{100,30},OppGping=new List<int>{10},Ripg=new List<string>{"default"}}};
        var rules=new RecoveredGameplayRules(config);var data=new PlayerData{SpinCount=0};int saves=0,notified=-1;
        var player=new RecoveredPlayerProgress(rules,()=>saves++,data);player.SpinCountChanged+=count=>notified=count;
        var ads=new LocalAdFacade();var view=new View();var claim=new RecoveredMoreSpinClaim(rules,player,data,ads,view);
        Assert.AreEqual(10,claim.BeforeShow());claim.Click("ClaimBtn");claim.Click("CloseBtn");Assert.AreEqual(0,view.Hides);
        Assert.AreEqual("extraspin",ads.Placement);Assert.AreEqual("extraspin",ads.Scene);ads.Complete(failure);
        Assert.IsFalse(claim.IsClicked);Assert.AreEqual(0,saves);claim.Click("ClaimBtn");
        data.SpinCount=95;config.Qonrii.OppGping[0]=12;ads.Complete(AdOutcome.Rewarded);
        Assert.AreEqual(100,player.SpinCount);Assert.AreEqual(107,notified);Assert.AreEqual(1,saves);Assert.AreEqual(1,view.Hides);
        Assert.IsTrue(claim.IsClicked);Assert.IsFalse(ads.Complete(AdOutcome.Rewarded));
    }
    [TestCase("default",30,false)]
    [TestCase("variant",29,false)]
    [TestCase("variant",30,true)]
    public void LimitIsInclusiveAndOnlyBlocksNonDefault(string type,int count,bool blocked)
    {
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Qonrii=new QonriiPoro{MojGping=new List<int>{100,30},OppGping=new List<int>{10},Ripg=new List<string>{type}}});
        var data=new PlayerData{LimitSpinCount=count};var player=new RecoveredPlayerProgress(rules,()=>{},data);
        var ads=new LocalAdFacade();var view=new View();var claim=new RecoveredMoreSpinClaim(rules,player,data,ads,view);
        claim.BeforeShow();claim.Click("ClaimBtn");Assert.AreEqual(blocked?1:0,view.Tips);Assert.AreEqual(!blocked,ads.Pending);
        Assert.IsTrue(claim.IsClicked);claim.Click("CloseBtn");Assert.AreEqual(0,view.Hides,"Native latch remains set after the limit tip.");
        claim.Cancel();if(ads.Pending)ads.Complete(AdOutcome.Rewarded);Assert.AreEqual(0,player.SpinCount);
        claim.BeforeShow();claim.Click("CloseBtn");Assert.AreEqual(1,view.Hides);
    }
}
