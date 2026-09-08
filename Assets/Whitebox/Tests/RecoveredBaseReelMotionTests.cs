using System.Collections.Generic;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredBaseReelMotionTests
{
    [Test]
    public void AccelerationEntersConstantSameFrameThenWaitsOneFrameBeforeReadingResult()
    {
        var state=Random.state;var catalog=Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");
        var reel=Object.Instantiate(Resources.Load<RecoveredReelView>("RecoveredSymbols/Reel"));
        try {
            reel.Initialize(catalog,RecoveredSlotType.Base);var motion=reel.GetComponent<RecoveredBaseReelMotion>();motion.enabled=false;
            Assert.IsTrue(motion.StartSlotSpin(1000,1));
            LogAssert.Expect(LogType.Error,"Already Start");Assert.IsFalse(motion.StartSlotSpin(5,1));
            motion.AdvanceMotion(0.5f);Assert.AreEqual(707.1068f,motion.CurrentSpeed,0.001f);Assert.AreEqual(-353.5534f,reel.OffsetPixels,0.001f);
            motion.AdvanceMotion(0.5f);Assert.AreEqual(-665.5534f,reel.OffsetPixels,0.001f,"Acceleration completion includes immediate constant-speed step.");
            int reads=0;var column=new List<int>{0,1,2};var tail=new[]{reel.SymbolId(3),reel.SymbolId(4),reel.SymbolId(5),reel.SymbolId(6)};
            motion.RequestStop(()=>{reads++;return column;});motion.AdvanceMotion(0.1f);
            Assert.AreEqual(0,reads);Assert.AreEqual(-665.5534f,reel.OffsetPixels,0.001f);
            column[0]=7;column[1]=9;column[2]=10;
            var randomBeforeResult=Random.state;
            motion.AdvanceMotion(0.1f);Assert.AreEqual(1,reads);
            CollectionAssert.AreEqual(new[]{7,9,10},new[]{reel.SymbolId(0),reel.SymbolId(1),reel.SymbolId(2)});
            CollectionAssert.AreEqual(tail,new[]{reel.SymbolId(3),reel.SymbolId(4),reel.SymbolId(5),reel.SymbolId(6)});
            for(int i=0;i<7;i++)Assert.AreSame(catalog.Find(reel.SymbolId(i)).LoadSprite(false),reel.SymbolAt(i).Symbol.sprite);
            Assert.AreEqual(randomBeforeResult,Random.state,"Result landing must not draw random symbols.");
            Assert.AreEqual(1.3311068f,motion.ReturnDuration,0.0001f);
            motion.AdvanceMotion(motion.ReturnDuration*0.5f);
            Assert.AreEqual(58.3681f,reel.OffsetPixels,0.01f,"OutBack overshoots the final position.");Assert.IsTrue(motion.IsSpinning);
            motion.AdvanceMotion(motion.ReturnDuration*0.5f);
            Assert.AreEqual(0,reel.OffsetPixels);Assert.IsFalse(motion.IsSpinning);Assert.IsFalse(motion.StopRequested);
            Assert.IsTrue(motion.StartSlotSpin(100,0.1f));
        } finally {Object.DestroyImmediate(reel.gameObject);Random.state=state;}
    }

    [Test]
    public void EarlyStopPreservesAccelerationAndZeroDistanceCompletesOnReturnUpdate()
    {
        var state=Random.state;var catalog=Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");
        var reel=Object.Instantiate(Resources.Load<RecoveredReelView>("RecoveredSymbols/Reel"));
        try {
            reel.Initialize(catalog,RecoveredSlotType.Base);var motion=reel.GetComponent<RecoveredBaseReelMotion>();motion.enabled=false;
            motion.StartSlotSpin(100,1);int reads=0;motion.RequestStop(()=>{reads++;return new[]{1,2,3};});
            motion.AdvanceMotion(0.5f);Assert.AreEqual(0,reads);
            motion.AdvanceMotion(0.5f);Assert.AreEqual(0,reads);Assert.AreEqual(-85.35534f,reel.OffsetPixels,0.001f);
            reel.SetOffsetPixels(0);motion.AdvanceMotion(0.1f);Assert.AreEqual(1,reads);Assert.AreEqual(0,motion.ReturnDuration);
            Assert.IsTrue(motion.IsSpinning);motion.AdvanceMotion(0.1f);Assert.IsFalse(motion.IsSpinning);
        } finally {Object.DestroyImmediate(reel.gameObject);Random.state=state;}
    }

    [Test]
    public void FailedResultProviderIsNotRetriedEveryFrame()
    {
        var state=Random.state;var catalog=Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");
        var reel=Object.Instantiate(Resources.Load<RecoveredReelView>("RecoveredSymbols/Reel"));
        try {
            reel.Initialize(catalog,RecoveredSlotType.Base);var motion=reel.GetComponent<RecoveredBaseReelMotion>();motion.enabled=false;
            motion.StartSlotSpin(100,0);int calls=0;motion.RequestStop(()=>{calls++;throw new System.InvalidOperationException("result");});
            motion.AdvanceMotion(0.1f);Assert.Throws<System.InvalidOperationException>(()=>motion.AdvanceMotion(0.1f));
            motion.AdvanceMotion(0.1f);Assert.AreEqual(1,calls);Assert.IsTrue(motion.IsSpinning,"Original failure never reaches the stop completion flags.");
        } finally {Object.DestroyImmediate(reel.gameObject);Random.state=state;}
    }
}
