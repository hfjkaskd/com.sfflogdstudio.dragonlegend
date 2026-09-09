using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredCashOutItemTests
{
    private static RecoveredGameplayRules Rules()
    {
        var c=new RgpggmPoro{Qogt=new List<int>{100},Rogk1tir=new List<int>{10},Rogk1roil=new List<int>{20},Rogk6tir=new List<int>{10},Rogk6roil=new List<int>{20},Rimgg1=new List<int>{7},Rimg7=new List<int>{7}};
        return new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rgpggm=c,Qollgqr=new QollgqrPoro{Ip=new List<int>{0,1,2},Lgtgl=new List<int>{1,1,1},Ronpom=new List<int>{1,1,1},Korrt=new List<int>{1,1,1}}});
    }
    [UnityTest]
    public IEnumerator ActualCardUsesLiveBalanceRecordTypeTaskCountsAndSharedSelectionFrame()
    {
        var item=Object.Instantiate(Resources.Load<RecoveredCashOutItem>("RecoveredUI/CashOutItem"));var frame=new GameObject("Shared frame",typeof(RectTransform));
        try
        {
            var rules=Rules();var data=new PlayerData{GreenCount=35};var player=new RecoveredPlayerProgress(rules,()=>Assert.Fail("Card must not save"),data);
            item.Bind(player,rules,()=>102);item.Initialize(0,0,1,(RectTransform)frame.transform,0);
            Assert.AreSame(item.transform,frame.transform.parent);Assert.AreEqual(Vector2.zero,((RectTransform)frame.transform).anchoredPosition);
            Assert.AreEqual(RecoveredCurrency.Format(100,0,0),item.AmountText.text);Assert.That(item.Fill.sizeDelta.x,Is.EqualTo(726*.35f).Within(.001f));
            Assert.AreEqual(RecoveredCurrency.Format(35,0,2)+"/"+RecoveredCurrency.Format(100,0,0),item.ProgressText.text);
            int selected=-1;item.SelectionRequested+=(id,shared)=>{selected=id;Assert.AreSame(frame.transform,shared);};item.Button.onClick.Invoke();Assert.AreEqual(0,selected);
            data.GreenCount=150;item.Initialize(0,-1,2,(RectTransform)frame.transform,0);Assert.AreEqual(726,item.Fill.sizeDelta.x);
            item.RefreshPaymentType(4);StringAssert.StartsWith("tx_icon_04",item.Button.transform.Find("Img").GetComponent<UnityEngine.UI.Image>().sprite.name);
            var record=new PlayerCashOutData{id=0,type=3,step=0,count=27,time=100,isCashout=false};data.PlayerCashOutDatas.Add(record);
            item.Initialize(0,0,1,(RectTransform)frame.transform,0);Assert.AreEqual("Spin 27/20 times",item.TaskText.text);Assert.AreEqual("Pending Review 00:00:02",item.TimeText.text);
            var icon=item.Button.transform.Find("Img").GetComponent<UnityEngine.UI.Image>();StringAssert.StartsWith("tx_icon_03",icon.sprite.name);
            item.RefreshPaymentType(4);StringAssert.StartsWith("tx_icon_03",icon.sprite.name);
            Assert.IsFalse(item.Fill.parent.gameObject.activeSelf);Assert.IsTrue(item.TaskText.transform.parent.gameObject.activeSelf);
            record.isCashout=true;item.Initialize(0,0,4,(RectTransform)frame.transform,0);Assert.AreEqual("Spin 27/10 times",item.TaskText.text);
            record.step=6;data.PlayerCollectDatas.Add(new PlayerCollectData{id=0,count=999});data.PlayerCollectDatas.Add(new PlayerCollectData{id=0,count=999});
            item.Initialize(0,0,1,(RectTransform)frame.transform,0);Assert.AreEqual("Collect 2/3 Treasures",item.TaskText.text);
            record.step=1000;item.Initialize(0,0,1,(RectTransform)frame.transform,0);Assert.IsFalse(item.TaskText.gameObject.activeSelf);Assert.IsFalse(item.TimeText.gameObject.activeSelf);
        }
        finally{item.Cancel();Object.Destroy(item.gameObject);Object.Destroy(frame);}
        yield return null;
    }
    [UnityTest]
    public IEnumerator CountdownPausesRunsWhileInactiveAndCompletesOnlyAtZero()
    {
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var item=Object.Instantiate(Resources.Load<RecoveredCashOutItem>("RecoveredUI/CashOutItem"));
        try
        {
            Time.timeScale=1;Time.captureDeltaTime=.1f;var rules=Rules();int now=102;
            var data=new PlayerData();data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,type=1,step=0,time=100});item.Bind(new RecoveredPlayerProgress(rules,()=>Assert.Fail("No save"),data),rules,()=>now);
            item.Initialize(0,-1,1,null,0);Assert.AreEqual("Pending Review 00:00:02",item.TimeText.text);int completed=0;item.CountdownCompleted+=()=>completed++;
            Time.timeScale=0;for(int i=0;i<15;i++)yield return null;Assert.AreEqual("Pending Review 00:00:02",item.TimeText.text);Assert.AreEqual(0,completed);
            Time.timeScale=1;item.gameObject.SetActive(false);for(int i=0;i<12;i++)yield return null;Assert.AreEqual("Pending Review 00:00:04",item.TimeText.text);
            item.TimeShow(-1);for(int i=0;i<45;i++)yield return null;Assert.AreEqual("Pending Review 00:00:00",item.TimeText.text);Assert.AreEqual(1,completed);
            item.TimeShow(1);item.TimeShow(0);for(int i=0;i<15;i++)yield return null;Assert.AreEqual(1,completed);
            item.TimeShow(1);item.Cancel();for(int i=0;i<15;i++)yield return null;Assert.AreEqual(1,completed);
            now=107;item.RefreshTime(true);Assert.AreEqual("Pending Review00:00:00",item.TimeText.text);item.RefreshTime();Assert.AreEqual("Pending Review 00:00:00",item.TimeText.text);
        }
        finally{item.Cancel();Object.Destroy(item.gameObject);Time.timeScale=scale;Time.captureDeltaTime=delta;}
        yield return null;
    }
}
