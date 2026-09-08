using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredRulesTests
{
    [Test]
    public void PreservesNativeRandomBoundaryIncludingZeroWeight()
    {
        var weights = new[] {0,2,3};
        Assert.AreEqual(0,RecoveredConfigCodec.SelectAt(weights,0));
        Assert.AreEqual(1,RecoveredConfigCodec.SelectAt(weights,2));
        Assert.AreEqual(2,RecoveredConfigCodec.SelectAt(weights,3));
        Assert.AreEqual(-1,RecoveredConfigCodec.SelectAt(new int[0],0));
    }
    [Test]
    public void BetUnlocksAtThresholdAndBUsesOnlyFirstBet()
    {
        var data = new GoldenDragonAutoGenConfig { Qonrii = new QonriiPoro {
            Rgr = new List<int>{1,5,10}, Rgrlgtgl = new List<int>{1,4,8} } };
        var rules = new RecoveredGameplayRules(data);
        var output = new List<int>();
        rules.GetBet(true,3,output); CollectionAssert.AreEqual(new[]{1},output);
        rules.GetBet(true,4,output); CollectionAssert.AreEqual(new[]{1,5},output);
        rules.GetBet(true,8,output); CollectionAssert.AreEqual(new[]{1,5,10},output);
        rules.GetBet(false,99,output); CollectionAssert.AreEqual(new[]{1},output);
        rules.GetBet(true,0,output); Assert.IsEmpty(output);
    }
    [UnityTest]
    public IEnumerator RealSnapshotRulesRetainFieldValuesAndNativeDispatch()
    {
        var loader = new ConfigSnapshotLoader();
        yield return loader.Load("RecoveredConfig/Remote/cp_default_1.json");
        var data = loader.Value;
        var rules = new RecoveredGameplayRules(data);
        Assert.AreEqual(10,rules.GetInitSpinCount());
        Assert.AreEqual(30,rules.GetLines());
        Assert.AreEqual(100f,rules.GetCashOutCash(0));
        Assert.AreEqual(5000f,rules.GetCashOutCash(2));
        Assert.AreSame(data.Gimrol.Rggl5,rules.ReelWeights(-1,0));
        Assert.AreSame(data.Gimrol.RgglKilp5,rules.ReelWeights(99,1));
        Assert.AreSame(data.Gimrol.MorgKilp1,rules.ReelWeights(0,2));
        Assert.AreEqual(data.Gimrol.J5[0],rules.GetPay(0,2));
        Assert.AreEqual(data.Rgpggm.Rogk1tir[0],rules.GetSuccessTaskCount(99,0));
        Assert.AreEqual(data.Rgpggm.Rogk6tir[1],rules.GetSuccessTaskCount(1,-1));
        Assert.AreEqual(data.Rgpggm.Rimg7[0],rules.GetWaitTime(-1,99));
    }
    [UnityTest]
    public IEnumerator EntryLoadsDefaultAndSwitchesProfileViaStandardButton()
    {
        var prefab = Resources.Load<GameObject>("Whitebox/GameEntry");
        Assert.IsNotNull(prefab);
        var go = Object.Instantiate(prefab);
        try
        {
            var entry = go.GetComponent<GameEntry>();
            float deadline = Time.realtimeSinceStartup + 15;
            while (entry.Rules == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsNotNull(entry.Rules);
            Assert.AreEqual("US_Default",entry.CurrentProfile.profileId);
            Assert.IsNotNull(entry.Settlement);
            var initialSettlement = entry.Settlement;
            Assert.IsTrue(entry.CurrentProfile.advertisementPresentationEnabled);
            int notifications = 0;
            entry.Ready += _ => notifications++;
            var button = go.transform.Find("SelectAlternative").GetComponent<Button>();
            Assert.AreEqual(0,button.onClick.GetPersistentEventCount());
            Assert.AreEqual(button.gameObject,button.targetGraphic.gameObject);
            button.onClick.Invoke();
            deadline = Time.realtimeSinceStartup + 15;
            while (entry.Rules == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual("A_Test",entry.CurrentProfile.profileId);
            Assert.IsNotNull(entry.Settlement);
            Assert.AreNotSame(initialSettlement, entry.Settlement);
            Assert.AreEqual(1,notifications);
            go.SetActive(false);
            Assert.IsNull(entry.Rules);
            Assert.IsNull(entry.Settlement);
        }
        finally { Object.Destroy(go); }
    }
}
