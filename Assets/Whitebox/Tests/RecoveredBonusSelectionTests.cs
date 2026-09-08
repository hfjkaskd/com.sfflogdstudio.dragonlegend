using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;

public sealed class RecoveredBonusSelectionTests
{
    private sealed class View:IRecoveredBonusSelectionView
    {
        public readonly List<string> Order=new List<string>();
        public readonly bool[] Ad=new bool[3],Enabled=new bool[3];
        public Action Release;public int Selected,Free,Plays,Hides;public bool Close;
        public void HideFinger()=>Order.Add("finger-hide");
        public void CancelFingerSequence()=>Order.Add("hint-cancel");
        public void InitializeCards(){Array.Clear(Ad,0,3);for(int i=0;i<3;i++)Enabled[i]=true;}
        public void SetCloseVisible(bool visible){Close=visible;Order.Add("close:"+visible);}
        public void SetCardEnabled(int index,bool enabled){Enabled[index]=enabled;Order.Add("button:"+enabled);}
        public void ShowCardAd(int index,bool visible){Ad[index]=visible;Order.Add("ad:"+index+":"+visible);}
        public void SetChances(int selected,int free){Selected=selected;Free=free;Order.Add("chances:"+selected);}
        public void PlayCard(int index,RecoveredBonusRound.Reveal reveal,Action releaseInput){Plays++;Release=releaseInput;Order.Add("play:"+index);}
        public void ShowFinger()=>Order.Add("finger-show");
        public void HideBonus(){Hides++;Order.Add("hide-bonus");}
    }
    private static RecoveredGameplayRules Rules(int free)=>new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Ronig=new RonigPoro {Ltoo=new List<int>{3},Qoi=new List<int>{0},Jin=new List<int>{0},Roo=new List<int>{0},Rgkorp=new List<int>{0},RrggRimgg=new List<int>{free}}});
    [Test]
    public void FreeThresholdPrecedesAnimationAndFinalCardWaitsForItsRealRelease()
    {
        var random=UnityEngine.Random.state;
        try {
            var view=new View();var ads=new LocalAdFacade();var selection=new RecoveredBonusSelection(Rules(1),ads,view,3);
            selection.BeforeShow();Assert.IsFalse(selection.NeedsAd);Assert.IsFalse(view.Close);Assert.AreEqual(0,view.Selected);
            view.Order.Clear();selection.Select(0);
            Assert.IsTrue(selection.IsClicked);Assert.IsTrue(selection.NeedsAd);Assert.IsTrue(view.Close);
            CollectionAssert.AreEqual(new[]{false,true,true},view.Ad);Assert.AreEqual(1,view.Selected);
            CollectionAssert.AreEqual(new[]{"finger-hide","hint-cancel","button:False","close:True","ad:1:True","ad:2:True","chances:1","play:0"},view.Order);
            selection.Select(1);Assert.AreEqual(1,view.Plays);Assert.IsFalse(ads.Pending);
            view.Release();selection.Select(1);Assert.IsTrue(ads.Pending);
            Assert.AreEqual("bonusCoin",ads.Placement);Assert.AreEqual("bonus",ads.Scene);Assert.AreEqual(1,selection.Round.ClickedCount);
            ads.Complete(AdOutcome.Rewarded);Assert.IsFalse(view.Ad[1]);Assert.AreEqual(2,view.Selected);view.Release();
            selection.Select(2);ads.Complete(AdOutcome.Rewarded);
            Assert.AreEqual(3,view.Selected);Assert.IsFalse(selection.IsEnd);Assert.AreEqual(0,view.Hides);
            view.Release();Assert.IsTrue(selection.IsEnd);Assert.IsFalse(selection.IsClicked);Assert.AreEqual(1,view.Hides);
            CollectionAssert.AreEqual(new[]{"hide-bonus","finger-show"},view.Order.GetRange(view.Order.Count-2,2));
        }finally{UnityEngine.Random.state=random;}
    }
    [TestCase(AdOutcome.Cancelled)]
    [TestCase(AdOutcome.Failed)]
    [TestCase(AdOutcome.Unavailable)]
    public void OriginalFailedAdRestoresButtonAndHintButDoesNotClearClickLatch(AdOutcome outcome)
    {
        var random=UnityEngine.Random.state;
        try {
            var view=new View();var ads=new LocalAdFacade();var selection=new RecoveredBonusSelection(Rules(0),ads,view,3);
            selection.BeforeShow();Assert.IsTrue(selection.NeedsAd);Assert.IsTrue(view.Close);
            selection.Select(0);ads.Complete(outcome);
            Assert.IsTrue(view.Enabled[0]);Assert.IsTrue(view.Ad[0]);Assert.IsTrue(selection.IsClicked);
            Assert.AreEqual(0,selection.Round.ClickedCount);Assert.AreEqual(0,view.Plays);
            selection.Select(1);Assert.IsFalse(ads.Pending);Assert.AreEqual("finger-show",view.Order[view.Order.Count-1]);
        }finally{UnityEngine.Random.state=random;}
    }
    [Test]
    public void BeforeShowRetainsStickyAdFlagAfterAnEarlierRoundReachedThreshold()
    {
        var random=UnityEngine.Random.state;
        try {
            var view=new View();var ads=new LocalAdFacade();var selection=new RecoveredBonusSelection(Rules(1),ads,view,3);
            selection.BeforeShow();selection.Select(0);view.Release();selection.BeforeShow();
            Assert.IsTrue(selection.NeedsAd);Assert.IsFalse(view.Close);CollectionAssert.AreEqual(new[]{false,false,false},view.Ad);
            selection.Select(0);Assert.IsTrue(ads.Pending);Assert.AreEqual(0,selection.Round.ClickedCount);
        }finally{UnityEngine.Random.state=random;}
    }
}
