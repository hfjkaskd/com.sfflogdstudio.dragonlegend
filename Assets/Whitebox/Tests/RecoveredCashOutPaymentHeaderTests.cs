using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredCashOutPaymentHeaderTests
{
    [UnityTest]
    public IEnumerator PaymentButtonsUpdateProviderPromptCardsFrameAndInitialBottomTierWithoutSaving()
    {
        var window=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/CashOutWindow"));
        try
        {
            var config=new GoldenDragonAutoGenConfig{Qonrii=new QonriiPoro{Ripg=new List<string>{"default"}},Rgpggm=new RgpggmPoro{Qogt=new List<int>{100,200}}};var rules=new RecoveredGameplayRules(config);var data=new PlayerData();var player=new RecoveredPlayerProgress(rules,()=>Assert.Fail("Header must not save"),data);
            var list=window.GetComponentInChildren<RecoveredCashOutList>(true);var bottom=window.GetComponentInChildren<RecoveredCashOutBottom>(true);var header=window.GetComponent<RecoveredCashOutPaymentHeader>();
            header.Bind(rules,player);header.RefreshMode(false);list.Scroll.enabled=false;list.Initialize(rules,player,bottom,()=>100,new Vector2(1080,620),1,0);window.SetActive(true);yield return null;
            var layout=window.transform.Find("Content/Node/Layout");Assert.IsTrue(layout.gameObject.activeSelf);Assert.AreEqual("Please enter your Paypal account here.",header.AccountInput.text);
            list.ItemAt(1).Button.onClick.Invoke();int sounds=0;header.SoundRequested+=value=>{Assert.AreEqual("click",value);sounds++;};
            var names=new[]{"PaypalBtn","CashAppBtn","CoinBaseBtn","ZelleBtn"};var providers=new[]{"Paypal","CashApp","CoinBase","Zelle"};
            for(int i=0;i<4;i++)
            {
                var button=layout.Find(names[i]).GetComponent<Button>();button.onClick.Invoke();Assert.AreEqual(i+1,header.PaymentType);Assert.AreSame(button.transform,header.PaymentFrame.parent);
                Assert.AreEqual("Please enter your "+providers[i]+" account here.",header.AccountInput.text);Assert.AreSame(list.ItemAt(1).transform,list.SelectionFrame.parent);
                StringAssert.EndsWith(RecoveredCurrency.Format(100,0,0),bottom.DetailText.text);StringAssert.StartsWith("tx_icon_0"+(i+1),list.ItemAt(0).Button.transform.Find("Img").GetComponent<Image>().sprite.name);
            }
            Assert.AreEqual(4,sounds);
            data.PlayerAccount.Add(new Account{type=4,accountName="Wrong field",emailName="first@example.test"});data.PlayerAccount.Add(new Account{type=4,emailName="second@example.test"});header.RefreshAccount(4);Assert.AreEqual("first@example.test",header.AccountInput.text);
            data.PlayerAccount[0].emailName=null;header.RefreshAccount(4);Assert.AreEqual(string.Empty,header.AccountInput.text);
            int requested=-1;header.CashAccountRequested+=type=>{requested=type;data.PlayerAccount[0].emailName="updated@example.test";};window.transform.Find("Content/Top/InputField (TMP)/AccountBtn").GetComponent<Button>().onClick.Invoke();Assert.AreEqual(4,requested);Assert.AreEqual("updated@example.test",header.AccountInput.text);
            header.RefreshAccount(-1);Assert.AreEqual("Please enter your Zelle account here.",header.AccountInput.text);
            config.Qonrii.Ripg[0]="US";header.RefreshMode(false);Assert.IsFalse(layout.gameObject.activeSelf,"Config type comparison is not a country switch.");
            header.RefreshAccount(3);header.RefreshMode(true);Assert.AreEqual("Please enter your account here.",header.AccountInput.text);Assert.IsFalse(layout.gameObject.activeSelf);
            data.GiftAccount=new GiftAccount{name="Not display name",address="Delivery address"};header.RefreshAccount(1);Assert.AreEqual(3,header.PaymentType);Assert.AreEqual("Delivery address",header.AccountInput.text);
            data.GiftAccount.address=null;header.RefreshAccount(1);Assert.AreEqual(string.Empty,header.AccountInput.text);
            int gifts=0;header.GiftAccountRequested+=()=>gifts++;window.transform.Find("Content/Top/InputField (TMP)/AccountBtn").GetComponent<Button>().onClick.Invoke();Assert.AreEqual(1,gifts);
        }
        finally{Object.Destroy(window);}
        yield return null;
    }
}
