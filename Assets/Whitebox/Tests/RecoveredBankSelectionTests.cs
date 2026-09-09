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
        public int Sounds;
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
        public void PlaySound(string name) { Assert.AreEqual("click",name); Sounds++; }
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
    [Test]
    public void ContinueAdUsesOneOffsetForRemainingBallAndCategoryAndUnlocksOnlyAtArrival()
    {
        var saved=Random.state;
        try
        {
            var config=new GoldenDragonAutoGenConfig {Qonrii=new QonriiPoro {
                RonkKgiitr=new List<int>{1,2,3},RonkMini=new List<int>{10,20,30},RonkMoj=new List<int>{12,22,32}}};
            for(int seed=0;seed<64;seed++)
            {
                Random.InitState(seed);var ads=new LocalAdFacade();var view=new View();
                var selection=new RecoveredBankSelection(new RecoveredGameplayRules(config),ads,view);selection.Reset();selection.Click(1);
                int category=selection.TempIndex;var state=Random.state;
                int offset=Random.Range(0,2);int expectedItem=new[]{0,2}[offset];
                var categories=new List<int>();for(int i=0;i<3;i++)if(i!=category)categories.Add(i);
                int expectedCategory=categories[offset];float expected=Random.Range(config.Qonrii.RonkMini[expectedCategory],config.Qonrii.RonkMoj[expectedCategory]+1);var after=Random.state;
                Random.state=state;selection.ClickButton("unknown");Assert.IsFalse(selection.IsContinue);
                selection.ClickButton("OpenBtn");Assert.IsTrue(selection.IsContinue);Assert.AreEqual(state,Random.state);
                Assert.AreEqual("bank",ads.Placement);Assert.AreEqual("Openbank",ads.Scene);
                selection.ClickButton("UnPlayBtn");Assert.AreEqual(0,view.Closed);Assert.AreEqual(1,view.Sounds);
                ads.Complete(AdOutcome.Failed);Assert.IsFalse(selection.IsContinue);Assert.AreEqual(state,Random.state);
                selection.ClickButton("OpenBtn");ads.Complete(AdOutcome.Rewarded);
                Assert.AreEqual((expectedItem,expected,false),view.Played[1]);Assert.AreEqual(after,Random.state);
                Assert.AreEqual(category,selection.TempIndex,"Continue does not overwrite the shared category drawn by manual selection.");
                CollectionAssert.AreEqual(new[]{1,expectedItem},view.Marked);Assert.IsTrue(selection.IsContinue);
                view.Arrivals[1](expected);Assert.IsFalse(selection.IsContinue);
                selection.ClickButton("UnPlayBtn");Assert.AreEqual(1,view.Closed);Assert.IsTrue(selection.IsContinue);
                Assert.AreEqual(1,ads.InterstitialCount);Assert.AreEqual("iv_close",ads.Placement);Assert.AreEqual("bank",ads.Scene);
                selection.Cancel();
            }
        }
        finally {Random.state=saved;}
    }
    [UnityTest]
    public IEnumerator ContinueFinalBallKeepsOriginalExcludedCategoryAndClosesAfterFlightDelay()
    {
        var saved=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        RecoveredBankSelection selection=null;
        try
        {
            Time.timeScale=0;Time.captureDeltaTime=.05f;
            var config=new GoldenDragonAutoGenConfig {Qonrii=new QonriiPoro {
                RonkKgiitr=new List<int>{1},RonkMini=new List<int>{10,20,30},RonkMoj=new List<int>{10,20,30}}};
            var ads=new LocalAdFacade();var view=new View();
            selection=new RecoveredBankSelection(new RecoveredGameplayRules(config),ads,view);selection.Reset();selection.Click(1);
            Assert.AreEqual(0,selection.TempIndex);
            selection.ClickButton("OpenBtn");ads.Complete(AdOutcome.Rewarded);view.Arrivals[1](view.Played[1].reward);
            Assert.IsFalse(selection.IsContinue);selection.ClickButton("OpenBtn");ads.Complete(AdOutcome.Rewarded);
            Assert.AreEqual(3,selection.Selected.Count);Assert.AreEqual(20,view.Played[2].reward,"One remaining ball forces offset zero; the category list still excludes only tempIndex=0.");
            Assert.AreEqual(0,selection.TempIndex);Assert.IsTrue(selection.IsContinue);Assert.AreEqual(0,view.Closed);
            view.Arrivals[2](20);for(int i=0;i<5;i++)yield return null;
            Assert.AreEqual(0,view.Closed);Assert.IsTrue(selection.IsContinue);
            Time.timeScale=1;for(int i=0;i<7;i++)yield return null;Assert.AreEqual(0,view.Closed);
            for(int i=0;i<8&&view.Closed==0;i++)yield return null;Assert.AreEqual(1,view.Closed);
            selection.Reset();selection.Click(0);selection.ClickButton("OpenBtn");
            var before=Random.state;int played=view.Played.Count;
            selection.Cancel();selection.Reset();ads.Complete(AdOutcome.Rewarded);
            Assert.AreEqual(before,Random.state);Assert.AreEqual(played,view.Played.Count);Assert.IsFalse(selection.IsContinue);
        }
        finally {selection?.Cancel();Random.state=saved;Time.timeScale=scale;Time.captureDeltaTime=delta;}
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
