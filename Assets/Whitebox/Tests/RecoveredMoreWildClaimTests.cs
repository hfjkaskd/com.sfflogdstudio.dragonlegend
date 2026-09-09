using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredMoreWildClaimTests
{
    private sealed class View : IRecoveredMoreWildView
    {
        public readonly List<string> Events = new List<string>();
        public void PlaySound(string sound) => Events.Add(sound);
        public void HideFinger() => Events.Add("finger");
        public void Hide() => Events.Add("hide");
    }

    [Test]
    public void FreeClaimAdvancesGuideBeforeNotificationAndSaveAndReplacesBalance()
    {
        var config = new GoldenDragonAutoGenConfig { Gimrol = new GimrolPoro { MorgKilpRimgg = new List<int> { 5 } } };
        var rules = new RecoveredGameplayRules(config);
        var data = new PlayerData { GuideStep = 2, MoreWild = 9 };
        var view = new View();
        var player = new RecoveredPlayerProgress(rules, () => view.Events.Add("save:" + data.GuideStep), data);
        player.MoreWildChanged += () => view.Events.Add("changed:" + data.MoreWild + ":" + data.GuideStep);
        var ads = new LocalAdFacade();
        var claim = new RecoveredMoreWildClaim(rules, player, data, ads, view);
        Assert.AreEqual(5, claim.BeforeShow(true));
        config.Gimrol.MorgKilpRimgg[0] = 7;
        claim.Click("ClaimBtn");
        CollectionAssert.AreEqual(new[] { "click", "finger", "changed:7:3", "save:3", "hide" }, view.Events);
        Assert.AreEqual(7, data.MoreWild);
        Assert.IsFalse(ads.Pending);
        Assert.IsFalse(claim.IsClicked);
    }

    [TestCase(AdOutcome.Cancelled)]
    [TestCase(AdOutcome.Unavailable)]
    [TestCase(AdOutcome.Failed)]
    public void AdFailureDoesNotGrantAndRetryUsesLiveConfigWithoutAdvancingGuide(AdOutcome failure)
    {
        var config = new GoldenDragonAutoGenConfig { Gimrol = new GimrolPoro { MorgKilpRimgg = new List<int> { 5 } } };
        var rules = new RecoveredGameplayRules(config);
        var data = new PlayerData { GuideStep = 2, MoreWild = 9 };
        int saves = 0;
        var player = new RecoveredPlayerProgress(rules, () => saves++, data);
        var view = new View(); var ads = new LocalAdFacade();
        var claim = new RecoveredMoreWildClaim(rules, player, data, ads, view);
        claim.BeforeShow(false); claim.Click("ClaimBtn");
        Assert.AreEqual("morewild", ads.Placement); Assert.AreEqual("morewild", ads.Scene);
        ads.Complete(failure);
        Assert.AreEqual(9, data.MoreWild); Assert.AreEqual(0, saves);
        claim.Click("ClaimBtn"); config.Gimrol.MorgKilpRimgg[0] = 3;
        ads.Complete(AdOutcome.Rewarded);
        Assert.AreEqual(3, data.MoreWild); Assert.AreEqual(2, data.GuideStep); Assert.AreEqual(1, saves);
        Assert.AreEqual("hide", view.Events[view.Events.Count - 1]);
    }

    [Test]
    public void PendingAdLeavesCloseAvailableAndGmCancellationRejectsOldReward()
    {
        var rules = new RecoveredGameplayRules(new GoldenDragonAutoGenConfig { Gimrol = new GimrolPoro { MorgKilpRimgg = new List<int> { 5 } } });
        var data = new PlayerData { GuideStep = 2 };
        var player = new RecoveredPlayerProgress(rules, () => Assert.Fail("Cancelled reward must not save"), data);
        var view = new View(); var ads = new LocalAdFacade();
        var claim = new RecoveredMoreWildClaim(rules, player, data, ads, view);
        claim.BeforeShow(false); claim.Click("Unknown"); Assert.IsEmpty(view.Events);
        claim.Click("ClaimBtn"); claim.Click("CloseBtn");
        CollectionAssert.AreEqual(new[] { "click", "finger", "click", "hide" }, view.Events);
        claim.Cancel(); ads.Complete(AdOutcome.Rewarded); claim.Click("ClaimBtn");
        Assert.AreEqual(0, data.MoreWild); Assert.AreEqual(2, data.GuideStep);
        Assert.AreEqual(4, view.Events.Count);
    }
}
