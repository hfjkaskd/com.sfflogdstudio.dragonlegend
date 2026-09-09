using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;

public sealed class RecoveredBankSelectionTests
{
    private sealed class View : IRecoveredBankSelectionView
    {
        public int Hides, Indicators;
        public int[] Marked;
        public readonly List<(int index,float reward,bool first)> Played = new List<(int,float,bool)>();
        public void HideFinger() => Hides++;
        public void ShowAdIndicators(IReadOnlyList<int> selected)
        {
            Indicators++; Marked = new int[selected.Count];
            for(int i=0;i<selected.Count;i++)Marked[i]=selected[i];
        }
        public void PlayItem(int index,float reward,bool first) => Played.Add((index,reward,first));
    }
    [TestCase(AdOutcome.Cancelled)]
    [TestCase(AdOutcome.Unavailable)]
    [TestCase(AdOutcome.Failed)]
    public void FirstPickIsFreeFailedLaterPickIsRemovedAndRetryRerollsBeforeAd(AdOutcome failure)
    {
        var saved=Random.state;
        try
        {
            var config=new GoldenDragonAutoGenConfig {Qonrii=new QonriiPoro {
                RonkKgiitr=new List<int>{1},RonkMini=new List<int>{11},RonkMoj=new List<int>{11}}};
            var rules=new RecoveredGameplayRules(config);var ads=new LocalAdFacade();var view=new View();
            var selection=new RecoveredBankSelection(rules,ads,view);selection.Reset();selection.Click(2);
            Assert.AreEqual((2,11f,true),view.Played[0]);Assert.IsFalse(ads.Pending);
            var afterFirst=Random.state;selection.Click(2);Assert.AreEqual(afterFirst,Random.state);
            Assert.AreEqual(1,view.Hides);selection.Click(0);
            CollectionAssert.AreEqual(new[]{2,0},view.Marked);Assert.AreEqual("bank",ads.Placement);Assert.AreEqual("itembank",ads.Scene);
            ads.Complete(failure);CollectionAssert.AreEqual(new[]{2},selection.Selected);
            Assert.AreEqual(1,view.Indicators,"Failure only removes the item index; it does not refresh the icons.");
            config.Qonrii.RonkMini[0]=config.Qonrii.RonkMoj[0]=23;selection.Click(0);
            config.Qonrii.RonkMini[0]=config.Qonrii.RonkMoj[0]=99;
            ads.Complete(AdOutcome.Rewarded);
            Assert.AreEqual((0,23f,false),view.Played[1],"Native captures the rolled reward before the advertisement.");
            selection.Click(1);selection.Cancel();ads.Complete(AdOutcome.Rewarded);
            Assert.AreEqual(2,view.Played.Count);selection.Click(9);Assert.AreEqual(4,view.Hides);
            selection.Reset();Assert.IsEmpty(selection.Selected);selection.Click(1);Assert.AreEqual((1,99f,true),view.Played[2]);
        }
        finally {Random.state=saved;}
    }
    [Test]
    public void RewardUsesInclusiveIntegerRangeAndPreservesNativeRandomDrawOrder()
    {
        var saved=Random.state;
        try
        {
            var config=new GoldenDragonAutoGenConfig {Qonrii=new QonriiPoro {
                RonkKgiitr=new List<int>{0,7,2},RonkMini=new List<int>{1,20,30},RonkMoj=new List<int>{3,22,32}}};
            var rules=new RecoveredGameplayRules(config);var ads=new LocalAdFacade();var view=new View();
            var selection=new RecoveredBankSelection(rules,ads,view);
            for(int seed=0;seed<64;seed++)
            {
                Random.InitState(seed);int expectedIndex=RecoveredGameplayRules.RandomListWeight(config.Qonrii.RonkKgiitr);
                float expected=Random.Range(config.Qonrii.RonkMini[expectedIndex],config.Qonrii.RonkMoj[expectedIndex]+1);var after=Random.state;
                Random.InitState(seed);selection.Reset();selection.Click(0);
                Assert.AreEqual(expectedIndex,selection.TempIndex);Assert.AreEqual(expected,view.Played[view.Played.Count-1].reward);Assert.AreEqual(after,Random.state);
            }
        }
        finally {Random.state=saved;}
    }
}
