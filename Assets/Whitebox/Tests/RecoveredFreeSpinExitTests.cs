using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredFreeSpinExitTests
{
    private static RecoveredGameplayRules Rules() => new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Rrggiomg=new RrggiomgPoro {QoinOmoinrKgiitr=new List<int>{1},RollOmoinrKgiitr=new List<int>{1}}});

    [Test]
    public void ZeroCountWaitsForPopupThenUsesSeparateTransitionCallbacks()
    {
        var rules=Rules(); int saves=0; var events=new List<string>();
        var progress=new RecoveredPlayerProgress(rules,()=>saves++,new PlayerData()) {
            GameSlotType=RecoveredSlotType.Free,FreeSpinCount=0,TotalFreeSpinWin=125};
        var entry=new RecoveredFreeSpinEntry(progress,new RecoveredFreeSpinResult(rules));
        var exit=new RecoveredFreeSpinExit(progress,entry);
        exit.PauseMusicRequested+=()=>{Assert.AreEqual(RecoveredSlotType.Base,progress.GameSlotType); events.Add("pause");};
        exit.EndViewRequested+=count=>{Assert.AreEqual(12,count); events.Add("popup");};
        exit.TransitionSoundRequested+=()=>events.Add("sound");
        exit.TransitionRequested+=()=>events.Add("transition");
        exit.BaseViewResetRequested+=()=>events.Add("view");
        exit.BaseReelsInitRequested+=()=>events.Add("reels");
        exit.FreeEndFlagClearRequested+=()=>events.Add("flag");
        exit.BaseMusicRequested+=()=>events.Add("music");
        exit.Check(12,null);
        Assert.IsTrue(exit.IsWaiting);
        exit.Check(12,null); exit.OnTransitionEvent(); exit.OnTransitionComplete();
        CollectionAssert.AreEqual(new[]{"pause","popup"},events);
        exit.CompleteEndView(); exit.CompleteEndView();
        CollectionAssert.AreEqual(new[]{"pause","popup","sound","transition"},events);
        exit.OnTransitionEvent();
        CollectionAssert.AreEqual(new[]{"pause","popup","sound","transition","view","reels"},events);
        exit.OnTransitionComplete(); exit.OnTransitionComplete();
        CollectionAssert.AreEqual(new[]{"pause","popup","sound","transition","view","reels","flag","music"},events);
        Assert.IsFalse(exit.IsWaiting); Assert.AreEqual(0,saves); Assert.AreEqual(125,progress.TotalFreeSpinWin);
    }

    [TestCase(2,1,true)]
    [TestCase(-1,-1,false)]
    public void NonzeroCountDelegatesToEntryWithoutOpeningEndView(int before,int after,bool generating)
    {
        var state=UnityEngine.Random.state;
        try {
            var rules=Rules(); int saves=0,popups=0;
            var progress=new RecoveredPlayerProgress(rules,()=>saves++,new PlayerData()) {
                GameSlotType=RecoveredSlotType.Free,FreeSpinCount=before};
            var result=new RecoveredFreeSpinResult(rules);
            var exit=new RecoveredFreeSpinExit(progress,new RecoveredFreeSpinEntry(progress,result));
            exit.EndViewRequested+=count=>popups++;
            exit.Check(12,new[]{0});
            Assert.AreEqual(after,progress.FreeSpinCount); Assert.AreEqual(generating,result.IsGenerating);
            Assert.AreEqual(0,popups); Assert.AreEqual(0,saves); Assert.IsFalse(exit.IsWaiting);
            Assert.AreEqual(RecoveredSlotType.Free,progress.GameSlotType);
        } finally {UnityEngine.Random.state=state;}
    }
}