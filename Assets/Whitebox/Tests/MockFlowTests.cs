using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using DragonLegend.Whitebox;

public sealed class MockFlowTests
{
    [UnityTest]
    public IEnumerator LoadsRealServerSnapshotWithNativeUnityPipeline()
    {
        var loader=new ConfigSnapshotLoader();
        yield return loader.Load("RecoveredConfig/Remote/cp_default_1.json");
        Assert.AreEqual(10,loader.Value.Qonrii.InirGping[0]);
        CollectionAssert.AreEqual(new[]{100,3000,5000},loader.Value.Rgpggm.Qogt);
        Assert.AreEqual(30,loader.Value.Gimrol.Lingg[0]);
    }

    [UnityTest]
    public IEnumerator PrefabButtonsCompleteAdAndSimulatedCashFlow()
    {
        var prefab=Resources.Load<GameObject>("Whitebox/MockFlow");
        Assert.IsNotNull(prefab);
        var root=Object.Instantiate(prefab);
        try
        {
            var view=root.GetComponent<MockFlowView>();
            int rewards=0,changes=0;
            MockCashOrder latest=null;
            view.Rewarded+=()=>rewards++;
            view.CashChanged+=order=>{changes++;latest=order;};
            root.SetActive(true);
            yield return null;
            var buttons=root.GetComponentsInChildren<Button>(true);
            Click(buttons,"RequestAd"); Click(buttons,"CompleteAd"); Click(buttons,"CompleteAd");
            Assert.AreEqual(1,rewards,"Reward must be delivered exactly once");
            Click(buttons,"SubmitCash");Assert.AreEqual(CashOutcome.Pending,latest.Outcome);
            Click(buttons,"ResolveCash");Assert.AreEqual(CashOutcome.SimulatedApproved,latest.Outcome);
            Assert.AreEqual(2,changes);
            root.SetActive(false);root.SetActive(true);yield return null;
            Click(buttons,"RequestAd");Click(buttons,"CompleteAd");Assert.AreEqual(2,rewards,"Re-enable must not duplicate listeners");
        }
        finally {Object.Destroy(root);}
    }

    [Test]
    public void FailureOutcomesAndCashRetriesAreIsolated()
    {
        var ads=new LocalAdFacade();int rewards=0,failures=0;
        foreach(var outcome in new[]{AdOutcome.Cancelled,AdOutcome.Failed,AdOutcome.Unavailable})
        {ads.PlayRewardAd(()=>rewards++,()=>failures++,"mock","test");ads.Complete(outcome);}
        Assert.AreEqual(0,rewards);Assert.AreEqual(3,failures);
        var cash=new LocalCashFacade();var first=cash.Submit("test",100);
        Assert.AreSame(first,cash.Submit("test",100));
        Assert.Throws<System.InvalidOperationException>(()=>cash.Submit("test",200));
        Assert.IsTrue(cash.Resolve("test",CashOutcome.Rejected));
        Assert.IsFalse(cash.Resolve("test",CashOutcome.SimulatedApproved));
    }

    private static void Click(Button[] buttons,string name)
    {
        foreach(var button in buttons) if(button.name==name){button.onClick.Invoke();return;}
        Assert.Fail("Missing standard Button: "+name);
    }
}
