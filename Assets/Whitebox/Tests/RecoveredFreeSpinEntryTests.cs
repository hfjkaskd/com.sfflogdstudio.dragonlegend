using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;

public sealed class RecoveredFreeSpinEntryTests
{
    private static RecoveredGameplayRules Rules() => new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Rrggiomg=new RrggiomgPoro {QoinOmoinrKgiitr=new List<int>{1},RollOmoinrKgiitr=new List<int>{1}}});

    [TestCase(0)]
    [TestCase(-1)]
    public void ExhaustedEntryReturnsBeforeInspectingSymbols(int count)
    {
        var rules=Rules(); int saves=0;
        var progress=new RecoveredPlayerProgress(rules,()=>saves++,new PlayerData()) {FreeSpinCount=count};
        var result=new RecoveredFreeSpinResult(rules);
        var entry=new RecoveredFreeSpinEntry(progress,result);
        Assert.IsFalse(entry.TryBegin(null));
        Assert.AreEqual(count,progress.FreeSpinCount); Assert.AreEqual(0,saves);
        Assert.IsFalse(result.IsGenerating);
    }

    [Test]
    public void FreeEntryDebitsBeforeGenerationAndRequestsPresentationOnlyOnCompletion()
    {
        var random=UnityEngine.Random.state;
        try {
            var rules=Rules(); var data=new PlayerData {SpinCount=8,LevelExpCount=3,JpAddCount=4,BankCount=5,MoreWild=6};
            string before=JsonUtility.ToJson(data); int saves=0,events=0;
            var progress=new RecoveredPlayerProgress(rules,()=>saves++,data) {FreeSpinCount=2};
            var result=new RecoveredFreeSpinResult(rules); var entry=new RecoveredFreeSpinEntry(progress,result);
            entry.PresentationRequested+=()=>{
                events++; Assert.IsFalse(result.IsGenerating); Assert.AreEqual(1,progress.FreeSpinCount);
                Assert.AreEqual(4,result.GetSymbol(4,2));
            };
            Assert.IsTrue(entry.TryBegin(new[]{4}));
            Assert.AreEqual(1,progress.FreeSpinCount); Assert.AreEqual(0,events);
            Assert.IsFalse(entry.TryBegin(new[]{4}));
            Assert.AreEqual(1,progress.FreeSpinCount);
            while(result.IsGenerating) result.Step();
            Assert.AreEqual(1,events); Assert.AreEqual(0,saves);
            Assert.AreEqual(before,JsonUtility.ToJson(data));
            Assert.IsFalse(result.Step()); Assert.AreEqual(1,events);
        } finally {UnityEngine.Random.state=random;}
    }

    [Test]
    public void GenerationFailureDoesNotRefundRuntimeDebit()
    {
        var rules=Rules(); int saves=0;
        var progress=new RecoveredPlayerProgress(rules,()=>saves++,new PlayerData()) {FreeSpinCount=2};
        var entry=new RecoveredFreeSpinEntry(progress,new RecoveredFreeSpinResult(rules));
        Assert.Throws<ArgumentNullException>(()=>entry.TryBegin(null));
        Assert.AreEqual(1,progress.FreeSpinCount); Assert.AreEqual(0,saves);
    }
}