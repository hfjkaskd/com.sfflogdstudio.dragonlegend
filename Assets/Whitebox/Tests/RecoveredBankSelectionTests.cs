using System;
using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Random=UnityEngine.Random;

public sealed class RecoveredBankSelectionTests
{
    private sealed class View : IRecoveredBankSelectionView
    {
        public int Hides, Indicators;
        public int WinCount => 3;
        public int Buttons, Fingers, Closed;
        public readonly List<Action<float>> Arrivals = new List<Action<float>>();
        public int[] Marked;
        public readonly List<(int index,float reward,bool first)> Played = new List<(int,float,bool)>();
        public void HideFinger() => Hides++;
        public void ShowAdIndicators(IReadOnlyList<int> selected)
        {
            Indicators++; Marked = new int[selected.Count];
            for(int i=0;i<selected.Count;i++)Marked[i]=selected[i];
        }
        public void PlayItem(int index,float reward,bool first,Action<float> completed) { Played.Add((index,reward,first)); Arrivals.Add(completed); }
        public void RevealButtons(float duration) { Assert.AreEqual(.5f,duration); Buttons++; }
        public void ShowContinueFinger() => Fingers++;
        public void Hide() => Closed++;
    }
    [UnityTest]
    public IEnumerator FlightArrivalRevealsButtonsAndFinalSelectionWaitsBeforeClosing()
    {
        var saved=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        RecoveredBankSelection selection=null;
        try
        {
            Time.timeScale=0;Time.captureDeltaTime=.05f;
            var config=new GoldenDragonAutoGenConfig {Qonrii=new QonriiPoro {
                RonkKgiitr=new List<int>{1},RonkMini=new List<int>{11},RonkMoj=new List<int>{11}}};
            var ads=new LocalAdFacade();var view=new View();
            selection=new RecoveredBankSelection(new RecoveredGameplayRules(config),ads,view);selection.Reset();
            selection.Click(0);Assert.AreEqual(0,view.Buttons);
            view.Arrivals[0](11);Assert.AreEqual(1,view.Buttons);CollectionAssert.AreEqual(new[]{0},view.Marked);
            for(int i=0;i<5;i++)yield return null;Assert.AreEqual(0,view.Fingers);
            Time.timeScale=1;for(int i=0;i<15;i++)yield return null;Assert.AreEqual(0,view.Fingers);
            for(int i=0;i<10&&view.Fingers==0;i++)yield return null;Assert.AreEqual(1,view.Fingers);
            selection.Click(1);ads.Complete(AdOutcome.Rewarded);view.Arrivals[1](11);
            for(int i=0;i<15;i++)yield return null;Assert.AreEqual(0,view.Closed,"A later arrival with fewer than WinCount selections does not close.");
            selection.Click(2);ads.Complete(AdOutcome.Rewarded);Assert.AreEqual(0,view.Closed);
            Time.timeScale=0;view.Arrivals[2](11);
            for(int i=0;i<5;i++)yield return null;Assert.AreEqual(0,view.Closed);
            Time.timeScale=1;for(int i=0;i<7;i++)yield return null;Assert.AreEqual(0,view.Closed);
            for(int i=0;i<8&&view.Closed==0;i++)yield return null;Assert.AreEqual(1,view.Closed);
            // Reset must also discard old ad/flight owners even though cancelled is false again.
            selection.Reset();selection.Click(0);int oldFirst=view.Arrivals.Count-1;
            selection.Click(1);selection.Cancel();selection.Reset();ads.Complete(AdOutcome.Rewarded);
            int played=view.Played.Count;view.Arrivals[oldFirst](11);
            Assert.AreEqual(oldFirst+1,played);Assert.AreEqual(1,view.Buttons);Assert.IsEmpty(selection.Selected);
            selection.Click(0);view.Arrivals[view.Arrivals.Count-1](11);selection.Cancel();selection.Reset();
            for(int i=0;i<25;i++)yield return null;Assert.AreEqual(1,view.Fingers,"Reset suppresses the delayed finger from the previous window lifetime.");
        }
        finally {selection?.Cancel();Random.state=saved;Time.timeScale=scale;Time.captureDeltaTime=delta;}
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
    [UnityTest]
    public IEnumerator LaterArrivalChecksSelectedCountOnceEvenWithAnotherAdvertisementPending()
    {
        var saved=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        RecoveredBankSelection selection=null;
        try
        {
            Time.timeScale=0;Time.captureDeltaTime=.05f;
            var config=new GoldenDragonAutoGenConfig {Qonrii=new QonriiPoro {
                RonkKgiitr=new List<int>{1},RonkMini=new List<int>{11},RonkMoj=new List<int>{11}}};
            var ads=new LocalAdFacade();var view=new View();
            selection=new RecoveredBankSelection(new RecoveredGameplayRules(config),ads,view);selection.Reset();
            selection.Click(0);selection.Click(1);ads.Complete(AdOutcome.Rewarded);selection.Click(2);
            Assert.IsTrue(ads.Pending);Assert.AreEqual(3,selection.Selected.Count);
            view.Arrivals[1](11); // Native compares selected.Count, not the number of completed flights.
            ads.Complete(AdOutcome.Failed);Assert.AreEqual(2,selection.Selected.Count);
            Time.timeScale=1;for(int i=0;i<15&&view.Closed==0;i++)yield return null;
            Assert.AreEqual(1,view.Closed,"The native half-second wait has no second count check after resuming.");
            Assert.AreEqual(0,view.Buttons,"The outstanding first flight has not revealed the buttons.");
        }
        finally {selection?.Cancel();Random.state=saved;Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }
}
