using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class RecoveredSpinEntryTests
{
    [UnityTest]
    public IEnumerator FirstSpinPersistsInNativeOrderAndStartsGuideResult()
    {
        var loader = new ConfigSnapshotLoader();
        yield return loader.Load("RecoveredConfig/Remote/cp_default_1.json");
        var state = UnityEngine.Random.state;
        try
        {
            var rules = new RecoveredGameplayRules(loader.Value);
            var data = new PlayerData {SpinCount=rules.GetMaxSpinCount(),MoreWild=1};
            var events = new List<string>();
            int savedTimestamp = -1;
            System.Action save = () => { events.Add("save"); savedTimestamp=data.LastSpinTime; };
            var progress = new RecoveredPlayerProgress(rules,save,data);
            var result = new RecoveredSpinResult(rules,new RecoveredSlotSettlement(rules));
            var entry = new RecoveredSpinEntry(rules,data,progress,result,save);
            entry.StartVisualsRequested += () => events.Add("start");
            entry.GuideHideRequested += () => events.Add("guide");
            progress.SpinCountChanged += _ => events.Add("spin");
            progress.LevelExperienceChanged += (a,b,c) => events.Add("xp");
            progress.BankReady += () => events.Add("bank");
            progress.BankProgressChanged += () => events.Add("bank");
            entry.WinResetRequested += () => events.Add("reset");
            entry.JackpotAnimationsRequested += () => events.Add("jackpots");
            progress.MoreWildChanged += () => events.Add("wild");
            entry.CountdownRequested += seconds => {
                Assert.AreEqual(rules.GetSpinCD(progress.Level),seconds); events.Add("timer");
            };
            Assert.IsTrue(entry.TryBegin(false,1,"default",123456));
            CollectionAssert.AreEqual(new[]{"start","guide","save","spin","xp","save",
                "save","bank","save","reset","jackpots","wild","save","timer"},events);
            Assert.AreEqual(2,data.GuideStep);
            Assert.AreEqual(0,progress.MoreWild);
            Assert.AreEqual(1,data.JpAddCount);
            Assert.AreEqual(1,data.BankCount);
            Assert.AreEqual(123456,data.LastSpinTime);
            Assert.AreEqual(0,savedTimestamp,"Timestamp is written after the last immediate save.");
            Assert.IsTrue(result.FirstFreeReward);
            Assert.IsTrue(result.IsGenerating);
            int previousSpinCount = progress.SpinCount;
            Assert.IsFalse(entry.TryBegin(false,1,"default",999));
            Assert.AreEqual(previousSpinCount,progress.SpinCount);
        }
        finally { UnityEngine.Random.state=state; }
    }

    [UnityTest]
    public IEnumerator BusyAndEmptySpinRequestsLeavePlayerRecordUnchanged()
    {
        var loader = new ConfigSnapshotLoader();
        yield return loader.Load("RecoveredConfig/Remote/cp_default_1.json");
        var rules = new RecoveredGameplayRules(loader.Value);
        var data = new PlayerData {SpinCount=0};
        int saves=0,more=0;
        System.Action save=()=>saves++;
        var progress=new RecoveredPlayerProgress(rules,save,data);
        var result=new RecoveredSpinResult(rules,new RecoveredSlotSettlement(rules));
        var entry=new RecoveredSpinEntry(rules,data,progress,result,save);
        entry.MoreSpinsRequested+=()=>more++;
        string before=UnityEngine.JsonUtility.ToJson(data);
        Assert.IsFalse(entry.TryBegin(true,1,"default",100));
        Assert.AreEqual(0,more);
        Assert.IsFalse(entry.TryBegin(false,1,"default",100));
        Assert.AreEqual(1,more);
        Assert.AreEqual(0,saves);
        Assert.AreEqual(before,UnityEngine.JsonUtility.ToJson(data));
        Assert.IsFalse(result.IsGenerating);
    }

    [TestCase(-1,false)] [TestCase(3,true)] [TestCase(8,true)]
    public void BankAssignmentDoesNotClampAndNotifiesEveryTimeBeforeSaving(int count,bool ready)
    {
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
            Qonrii=new QonriiPoro {RonkGpinQP=new List<int>{3}}
        });
        var events=new List<string>();
        var data=new PlayerData();
        var progress=new RecoveredPlayerProgress(rules,()=>events.Add("save"),data);
        progress.BankReady+=()=>events.Add("ready");
        progress.BankProgressChanged+=()=>events.Add("progress");
        progress.SetBankCount(count);
        progress.SetBankCount(count);
        Assert.AreEqual(count,data.BankCount);
        string expected=ready ? "ready" : "progress";
        CollectionAssert.AreEqual(new[]{expected,"save",expected,"save"},events);
    }
}
