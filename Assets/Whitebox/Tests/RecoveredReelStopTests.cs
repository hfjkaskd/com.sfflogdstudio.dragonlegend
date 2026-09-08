using System;
using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

public sealed class RecoveredReelStopTests
{
    private static RecoveredReelView Create()
    {
        var reel=UnityEngine.Object.Instantiate(Resources.Load<RecoveredReelView>("RecoveredSymbols/Reel"));
        reel.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),RecoveredSlotType.Base);
        reel.GetComponent<RecoveredBaseReelMotion>().enabled=false;return reel;
    }
    private static void Finish(RecoveredBaseReelMotion motion)
    {
        motion.AdvanceMotion(0.02f);motion.AdvanceMotion(0.02f);motion.AdvanceMotion(motion.ReturnDuration);
        Assert.IsFalse(motion.IsSpinning);Assert.IsFalse(motion.StopRequested);
    }
    [UnityTest]
    public IEnumerator ScaledDelayWaitsThroughPauseAndCallbackFollowsClearedStopFlag()
    {
        var state=Random.state;float scale=Time.timeScale;var reel=Create();
        try {
            var motion=reel.GetComponent<RecoveredBaseReelMotion>();motion.StartSlotSpin(100,0.01f);
            int calls=0;RecoveredReelStopOperation operation=null;
            Time.timeScale=0;
            operation=motion.SetStop(0.04f,()=>new[]{7,9,10},view=>{
                Assert.AreSame(reel,view);Assert.IsFalse(motion.StopRequested);Assert.IsFalse(operation.IsCompleted);calls++;
            });
            Assert.IsFalse(motion.StopRequested);
            yield return null;yield return null;yield return null;
            Assert.IsFalse(motion.StopRequested);Assert.AreEqual(0,calls);
            Time.timeScale=1;float deadline=Time.realtimeSinceStartup+5;
            while(!motion.StopRequested&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsTrue(motion.StopRequested);Assert.IsFalse(operation.IsCompleted);
            Finish(motion);Assert.AreEqual(0,calls,"Callback belongs to the next native Update runner pass.");
            yield return null;
            Assert.IsTrue(operation.IsCompleted);Assert.AreEqual(1,calls);operation.GetResult();
            yield return null;Assert.AreEqual(1,calls);
        } finally {Time.timeScale=scale;UnityEngine.Object.Destroy(reel.gameObject);Random.state=state;}
    }
    [UnityTest]
    public IEnumerator ConcurrentZeroDelayCallsKeepBothCallbacksAndSkipCreationFrame()
    {
        var state=Random.state;var reel=Create();
        try {
            var motion=reel.GetComponent<RecoveredBaseReelMotion>();motion.StartSlotSpin(100,0.01f);
            var calls=new List<int>();
            var first=motion.SetStop(0,()=>new[]{1,2,3},_=>calls.Add(1));
            var second=motion.SetStop(0,()=>new[]{7,9,10},_=>calls.Add(2));
            Assert.IsFalse(motion.StopRequested);Assert.IsFalse(first.IsCompleted);Assert.IsFalse(second.IsCompleted);
            yield return null;Assert.IsTrue(motion.StopRequested);Assert.IsEmpty(calls);
            Finish(motion);yield return null;
            CollectionAssert.AreEqual(new[]{1,2},calls);Assert.IsTrue(first.IsCompleted);Assert.IsTrue(second.IsCompleted);
            Assert.AreEqual(7,reel.SymbolId(0)); // The current result provider is read only at landing.
        } finally {UnityEngine.Object.Destroy(reel.gameObject);Random.state=state;}
    }
    [UnityTest]
    public IEnumerator DifferentDelaysPreserveOriginalTailCompactionCallbackOrder()
    {
        var state=Random.state;float capture=Time.captureDeltaTime;float scale=Time.timeScale;var reel=Create();
        try {
            Time.timeScale=1;Time.captureDeltaTime=0.01f;
            yield return null;
            var motion=reel.GetComponent<RecoveredBaseReelMotion>();motion.StartSlotSpin(100,0.01f);
            var calls=new List<int>();
            motion.SetStop(0.025f,()=>new[]{1,2,3},_=>calls.Add(1));
            motion.SetStop(0,()=>new[]{1,2,3},_=>calls.Add(2));
            motion.SetStop(0,()=>new[]{1,2,3},_=>calls.Add(3));
            yield return null;yield return null;yield return null;yield return null;
            Assert.IsEmpty(calls);Finish(motion);yield return null;
            CollectionAssert.AreEqual(new[]{3,1,2},calls);
        } finally {Time.captureDeltaTime=capture;Time.timeScale=scale;UnityEngine.Object.Destroy(reel.gameObject);Random.state=state;}
    }
    [UnityTest]
    public IEnumerator CallbackFaultIsObservableAndDoesNotPreventAnotherWaiterCompleting()
    {
        var state=Random.state;var reel=Create();
        try {
            var motion=reel.GetComponent<RecoveredBaseReelMotion>();motion.StartSlotSpin(100,0.01f);int calls=0;
            var failed=motion.SetStop(0,()=>new[]{0,1,2},_=>{calls++;throw new InvalidOperationException("callback");});
            var successful=motion.SetStop(0,()=>new[]{0,1,2});
            yield return null;Finish(motion);yield return null;
            Assert.IsTrue(failed.IsCompleted);Assert.IsTrue(successful.IsCompleted);
            Assert.Throws<InvalidOperationException>(()=>failed.GetResult());Assert.DoesNotThrow(()=>successful.GetResult());
            yield return null;Assert.AreEqual(1,calls);
        } finally {UnityEngine.Object.Destroy(reel.gameObject);Random.state=state;}
    }
}
