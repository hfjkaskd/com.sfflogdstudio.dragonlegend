using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredCashOutModeViewTests
{
    [UnityTest]
    public IEnumerator ActualTabButtonsPreserveNativeLayoutAccountAndRepeatClickBoundaries()
    {
        var window=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/CashOutWindow"));
        try {
            var config=new GoldenDragonAutoGenConfig{Qonrii=new QonriiPoro{Ripg=new List<string>{"default"}}};
            var rules=new RecoveredGameplayRules(config);var data=new PlayerData();string saved=JsonUtility.ToJson(data);
            var player=new RecoveredPlayerProgress(rules,()=>Assert.Fail("Tabs must not save"),data);
            var view=window.GetComponent<RecoveredCashOutModeView>();var header=window.GetComponent<RecoveredCashOutPaymentHeader>();
            var bottom=window.GetComponentInChildren<RecoveredCashOutBottom>(true);
            var node=window.transform.Find("Content/Node");var cash=node.Find("Rect");var gift=node.Find("GiftRect");var layout=node.Find("Layout");
            int sounds=0,refreshes=0;view.SoundRequested+=name=>{Assert.AreEqual("click",name);sounds++;};
            view.ItemsRefreshRequested+=isGift=>{
                Assert.AreEqual(isGift,view.IsGift);Assert.AreEqual(isGift,gift.gameObject.activeSelf);
                Assert.AreEqual(!isGift,cash.gameObject.activeSelf);Assert.AreEqual(!isGift,bottom.gameObject.activeSelf);
                Assert.AreEqual(!isGift,view.CashButton.transform.GetChild(2).gameObject.activeSelf);
                Assert.AreEqual(isGift,view.GiftButton.transform.GetChild(2).gameObject.activeSelf);refreshes++;
            };
            view.Bind(rules,player);window.SetActive(true);view.ResetForShow();yield return null;
            Assert.AreEqual(1,refreshes);Assert.IsTrue(layout.gameObject.activeSelf);
            Assert.AreEqual("Please enter your Paypal account here.",header.AccountInput.text);
            view.CashButton.onClick.Invoke();Assert.AreEqual(1,sounds);Assert.AreEqual(1,refreshes);
            view.GiftButton.onClick.Invoke();Assert.AreEqual(2,sounds);Assert.AreEqual(2,refreshes);
            Assert.IsFalse(layout.gameObject.activeSelf);Assert.AreEqual("Please enter your account here.",header.AccountInput.text);
            view.GiftButton.onClick.Invoke();Assert.AreEqual(3,sounds);Assert.AreEqual(2,refreshes);
            view.CashButton.onClick.Invoke();Assert.AreEqual(4,sounds);Assert.AreEqual(3,refreshes);Assert.IsTrue(layout.gameObject.activeSelf);
            config.Qonrii.Ripg[0]="US";view.Refresh();Assert.IsFalse(layout.gameObject.activeSelf);
            Assert.IsTrue(bottom.gameObject.activeSelf);Assert.IsTrue(cash.gameObject.activeSelf);
            view.GiftButton.onClick.Invoke();view.ResetForShow();Assert.IsFalse(view.IsGift);
            Assert.AreEqual(5,sounds);Assert.AreEqual(6,refreshes);Assert.AreEqual(saved,JsonUtility.ToJson(data));
        } finally {Object.Destroy(window);}
        yield return null;
    }
}
