using System;
using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public sealed class RecoveredReelSpinTests
{
    private static RecoveredReelView Create()
    {
        var reel = Object.Instantiate(Resources.Load<RecoveredReelView>("RecoveredSymbols/Reel"));
        reel.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"), RecoveredSlotType.Base);
        reel.GetComponent<RecoveredBaseReelMotion>().enabled = false;
        return reel;
    }
    [TestCase(-1, 0)]
    [TestCase(2, 1)]
    public void ControlledStartOnlyCallsSelectedColumnAndRestoresDetachedSymbol(int selected, int expectedCalls)
    {
        var state = Random.state; var reel = Create(); var detached = new GameObject("Detached win symbol");
        try {
            var symbol = reel.SymbolAt(0).Symbol;
            symbol.transform.SetParent(detached.transform, false);
            symbol.transform.localPosition = new Vector3(5, 9, 3);
            symbol.transform.localScale = new Vector3(2, 2, 2);
            var sprite = symbol.sprite; int id = reel.SymbolId(0); int calls = 0;
            var motion = reel.GetComponent<RecoveredBaseReelMotion>();
            var operation = motion.StartBaseSpin(1, 2, () => throw new Exception("Must not read results"),
                index => { Assert.AreEqual(2, index); Assert.IsTrue(motion.IsSpinning); calls++; },
                _ => Assert.Fail("Controlled startup does not await stop or call stopCall."), selected);
            Assert.IsTrue(operation.IsCompleted); operation.GetResult(); Assert.AreEqual(expectedCalls, calls);
            Assert.IsFalse(motion.StopRequested);
            Assert.AreSame(reel.SymbolAt(0).transform, symbol.transform.parent);
            Assert.AreEqual(new Vector3(0, 0.86f, 3), symbol.transform.localPosition);
            Assert.AreEqual(new Vector3(2, 2, 2), symbol.transform.localScale);
            Assert.AreSame(sprite, symbol.sprite); Assert.AreEqual(id, reel.SymbolId(0));
            motion.AdvanceMotion(0.5f);
            Assert.AreEqual(7000f * Mathf.Sin(Mathf.PI / 4), motion.CurrentSpeed, 0.001f);
        } finally { Object.DestroyImmediate(reel.gameObject); Object.DestroyImmediate(detached); Random.state = state; }
    }
    [UnityTest]
    public IEnumerator AutomaticStartAwaitsHalfSecondStopAndAnAdditionalUpdateBeforeCallback()
    {
        var state = Random.state; float capture = Time.captureDeltaTime; float scale = Time.timeScale; var reel = Create();
        try {
            Time.timeScale = 1; Time.captureDeltaTime = 0.125f;
            yield return null;
            var motion = reel.GetComponent<RecoveredBaseReelMotion>(); int calls = 0;
            RecoveredReelSpinOperation operation = null;
            operation = motion.StartBaseSpin(0.01f, 4, () => new[] { 7, 9, 10 }, null, index => {
                Assert.AreEqual(4, index); Assert.IsFalse(operation.IsCompleted); calls++;
                throw new InvalidOperationException("stopCall");
            });
            Assert.IsFalse(operation.IsCompleted); Assert.IsFalse(motion.StopRequested);
            yield return null; yield return null; yield return null;
            Assert.IsFalse(motion.StopRequested);
            yield return null;
            Assert.IsTrue(motion.StopRequested);
            motion.AdvanceMotion(0.02f); motion.AdvanceMotion(0.02f); motion.AdvanceMotion(motion.ReturnDuration);
            Assert.IsFalse(motion.StopRequested); Assert.AreEqual(7, reel.SymbolId(0));
            yield return null; // SetStop completes and queues the outer WaitUntil.
            Assert.IsFalse(operation.IsCompleted); Assert.AreEqual(0, calls);
            yield return null;
            Assert.IsTrue(operation.IsCompleted); Assert.AreEqual(1, calls);
            Assert.Throws<InvalidOperationException>(() => operation.GetResult());
            yield return null; Assert.AreEqual(1, calls);
        } finally { Time.captureDeltaTime = capture; Time.timeScale = scale; Object.Destroy(reel.gameObject); Random.state = state; }
    }
}
