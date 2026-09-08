using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;

public sealed class RecoveredPlayerStoreTests
{
    private string key;
    [SetUp] public void SetUp() => key = "DragonLegend.Tests." + Guid.NewGuid().ToString("N");
    [TearDown] public void TearDown() { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }

    [Test]
    public void FreshRecordHasOriginalDefaultsAndCallbackIsSaved()
    {
        var store = new RecoveredPlayerStore(key);
        store.Load(status=>{
            Assert.AreEqual(0,status); Assert.AreEqual(1,store.Data.Level);
            Assert.AreEqual(1,store.Data.GuideStep); Assert.AreEqual(-1,store.Data.RandomIndex);
            Assert.IsTrue(store.Data.IsMusic); Assert.IsTrue(store.Data.IsVibrate);
            CollectionAssert.AreEqual(new[]{0,0,0,0,0},store.Data.BonusArea);
            store.Data.SpinCount=7;
        });
        var reload = new RecoveredPlayerStore(key);
        reload.Load(status=>Assert.AreEqual(1,status));
        Assert.AreEqual(7,reload.Data.SpinCount);
    }
    [Test]
    public void ProgressSavePreservesAllOtherPlayerSystems()
    {
        var store=new RecoveredPlayerStore(key); store.Load(null);
        store.Data.PlayerTaskDatas.Add(new PlayerTaskData{id=12,count=4,isRecieve=true});
        store.Data.PlayerCollectDatas.Add(new PlayerCollectData{id=3,count=8});
        store.Data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=2,type=1,step=5,count=9,time=17,isCashout=true});
        store.Data.PlayerAccount.Add(new Account{accountName="test",emailName="test@example.invalid",type=2});
        store.Data.PlayerCashOutOrders.Add(new PlayerCashOutOrder{orderId="test-order",index=4,status="pending"});
        store.Data.GiftAccount=new GiftAccount{name="test",address="test",code="123",phone="000"};
        store.Data.GuideStep=4; store.Data.BonusArea[2]=9; store.Data.GreenCount=125.5f;
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Qonrii=new QonriiPoro{MojGping=new List<int>{10}}});
        var progress=new RecoveredPlayerProgress(rules,store.Save,store.Data);
        progress.SetSpinCount(8);
        var reload=new RecoveredPlayerStore(key); reload.Load(null);
        Assert.AreEqual(JsonUtility.ToJson(store.Data),JsonUtility.ToJson(reload.Data));
        Assert.AreEqual(8,reload.Data.SpinCount); Assert.AreEqual(125.5f,reload.Data.GreenCount);
    }
    [Test]
    public void MalformedJsonUsesOriginalRecoveryStatus()
    {
        PlayerPrefs.SetString(key,"{ invalid json");
        var store=new RecoveredPlayerStore(key);
        store.Load(status=>{Assert.AreEqual(2,status); store.Data.SpinCount=6;});
        var reload=new RecoveredPlayerStore(key); reload.Load(status=>Assert.AreEqual(1,status));
        Assert.AreEqual(6,reload.Data.SpinCount);
    }
}
