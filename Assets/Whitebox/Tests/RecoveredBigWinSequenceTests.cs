using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredBigWinSequenceTests
{
    private static RecoveredGameplayRules Rules()=>new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{
        Qonrii=new QonriiPoro{RiikinQloim=new List<int>{0,1000}}});
    [UnityTest]
    public IEnumerator NoLineWaitsWithoutFlightAndZeroReturnedAwardStillCountsAndWaits()
    {
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/SpinPlayfield"));var field=root.GetComponent<RecoveredSpinPlayfield>();
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var rules=Rules();var ads=new LocalAdFacade();
        var progress=new RecoveredPlayerProgress(rules,()=>{},new PlayerData{GreenCount=100});
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;field.SymbolAmount.Bind(0);field.DownWin.Bind(0);
            bool completed=false;float returned=-1,finished=-1,began=Time.time;
            field.BigWinPopup.FlyCoinRequested+=value=>{Assert.AreEqual(0,value);returned=Time.time;};
            field.BigWinSequence.Begin(RecoveredSlotWinType.Big,0,4,()=>4,progress,rules,ads,true,0,()=>{completed=true;finished=Time.time;});
            Assert.IsFalse(field.BigWinSequence.IsFlying);Assert.IsFalse(field.BigWinPopup.gameObject.activeSelf);
            Time.timeScale=0;for(int i=0;i<10;i++)yield return null;
            Assert.IsFalse(field.BigWinPopup.gameObject.activeSelf);Time.timeScale=1;
            for(int i=0;i<25&&!field.BigWinPopup.gameObject.activeSelf;i++)yield return null;
            Assert.IsTrue(field.BigWinPopup.gameObject.activeSelf);Assert.GreaterOrEqual(Time.time-began,.8f-.001f);
            Assert.AreEqual("GOOD LUCK",field.DownWin.Label.text);Assert.AreEqual(0,field.WinFlight.ActiveBurstCount);
            field.BigWinPopup.ClaimButton.onClick.Invoke();ads.Complete(AdOutcome.Rewarded);
            for(int i=0;i<30&&returned<0;i++)yield return null;
            Assert.GreaterOrEqual(returned,0);Assert.IsFalse(completed);
            for(int i=0;i<40&&!completed;i++)yield return null;
            Assert.IsTrue(completed);Assert.GreaterOrEqual(finished-returned,1.3f-.001f);
            Assert.AreEqual(0,field.DownWin.TemporaryTotal);Assert.AreEqual(100,progress.GreenCount);
            Assert.IsFalse(field.OrdinaryWin.IsFlying);Assert.IsNull(field.BigWinSequence.Error);
        }finally{Time.timeScale=scale;Time.captureDeltaTime=delta;Object.Destroy(root);}
    }
    [UnityTest]
    public IEnumerator CancellationRestoresFlyingLabelAndPreventsDelayedPopup()
    {
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/SpinPlayfield"));var field=root.GetComponent<RecoveredSpinPlayfield>();
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var rules=Rules();
        var progress=new RecoveredPlayerProgress(rules,()=>{},new PlayerData{GreenCount=100});
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;field.SymbolAmount.Bind(0);field.DownWin.Bind(0);
            var origin=field.SymbolAmount.Label.transform.position;
            field.BigWinSequence.Begin(RecoveredSlotWinType.Big,4,4,()=>0,progress,rules,new LocalAdFacade(),true,0,()=>Assert.Fail("Cancelled continuation"));
            yield return null;Assert.IsTrue(field.BigWinSequence.IsFlying);field.BigWinSequence.Cancel();
            Assert.AreEqual(origin,field.SymbolAmount.Label.transform.position);
            for(int i=0;i<40;i++)yield return null;
            Assert.IsFalse(field.BigWinPopup.gameObject.activeSelf);Assert.IsFalse(field.BigWinSequence.IsRunning);
            Assert.AreEqual(100,progress.GreenCount);
        }finally{Time.timeScale=scale;Time.captureDeltaTime=delta;Object.Destroy(root);}
    }
}
