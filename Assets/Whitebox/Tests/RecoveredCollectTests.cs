using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using Random=UnityEngine.Random;

public sealed class RecoveredCollectTests
{
    private static QollgqrPoro Config() => new QollgqrPoro {
        Ip=new List<int>{2,7,13},Lgtgl=new List<int>{4,8,12},
        Ronpom=new List<int>{99,0,0},Korrt=new List<int>{100,200,300},
        QollgqrQloim=new List<int>{1500,500},QollgqrRgkorp=new List<int>{123456}};

    [Test]
    public void CollectInfoCachesCompleteRecordsInOriginalOrder()
    {
        var c=Config();var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Qollgqr=c});
        Assert.AreEqual(3,rules.GetCollectInfoCount());var records=rules.GetCollectInfos();
        for(int i=0;i<3;i++) {
            Assert.AreEqual(c.Ip[i],records[i].id);Assert.AreEqual(c.Lgtgl[i],records[i].level);
            Assert.AreEqual(c.Ronpom[i],records[i].random);Assert.AreEqual(c.Korrt[i],records[i].worth);
        }
        c.Ip.Clear();c.Lgtgl[0]=100;c.Ronpom[0]=50;c.Korrt[0]=900;
        Assert.AreSame(records,rules.GetCollectInfos());Assert.AreEqual(3,rules.GetCollectInfoCount());
        Assert.AreEqual(2,records[0].id);Assert.AreEqual(4,records[0].level);
        Assert.AreEqual(99,records[0].random);Assert.AreEqual(100,records[0].worth);
    }

    [Test]
    public void MalformedCollectColumnsLeaveTheNativePartiallyBuiltCache()
    {
        var c=Config();c.Korrt.RemoveRange(1,2);
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Qollgqr=c});
        Assert.Throws<ArgumentOutOfRangeException>(()=>rules.GetCollectInfos());
        Assert.AreEqual(1,rules.GetCollectInfoCount());Assert.AreEqual(2,rules.GetCollectInfos()[0].id);
    }

    [Test]
    public void CollectDrawUsesLiveIdsAsWeightsAndReturnsIdRatherThanOrdinal()
    {
        var state=Random.state;var c=Config();
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Qollgqr=c});
        rules.GetCollectInfos();
        try {
            for(int seed=0;seed<64;seed++) {
                Random.InitState(seed);int draw=Random.Range(0,22);
                int expected=draw<=2?2:draw<=9?7:13;int next=Random.Range(0,1000000);
                Random.InitState(seed);Assert.AreEqual(expected,rules.RandomCollectIndex());
                Assert.AreEqual(next,Random.Range(0,1000000));
            }
            c.Ip=new List<int>{31};Assert.AreEqual(31,rules.RandomCollectIndex());
            c.Ip=new List<int>{0,0};Assert.AreEqual(0,rules.RandomCollectIndex());
            c.Ip.Clear();Assert.Throws<ArgumentOutOfRangeException>(()=>rules.RandomCollectIndex());
        } finally {Random.state=state;}
    }

    [Test]
    public void CollectRewardIsFormattedAtZeroDecimalsAndClaimReadsLiveThousandths()
    {
        var c=Config();var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Qollgqr=c});
        Assert.AreEqual(1.5f,rules.GetCollectClaim(0));Assert.AreEqual(.5f,rules.GetCollectClaim(1));
        Assert.AreEqual("$1,235",rules.GetCollectReward(0));Assert.AreEqual("R$1,235",rules.GetCollectReward(1));
        c.QollgqrQloim[0]=-125;c.QollgqrRgkorp[0]=100;
        Assert.AreEqual(-.125f,rules.GetCollectClaim(0));Assert.AreEqual("$1",rules.GetCollectReward(0));
    }

    [Test]
    public void CollectWritesPersistNativeFirstMatchAndReceivedBehavior()
    {
        var data=new PlayerData {RandomIndex=2};var saved=new List<string>();
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Qollgqr=Config()});
        var player=new RecoveredPlayerProgress(rules,()=>saved.Add(JsonUtility.ToJson(data)),data);
        Assert.AreSame(data.PlayerCollectDatas,player.CollectRecords);Assert.AreEqual(2,player.RandomIndex);
        player.SetCollectData(7,99);
        Assert.AreEqual(1,player.CollectRecords[0].count);Assert.IsFalse(player.CollectRecords[0].isRecieve);
        data.PlayerCollectDatas.Add(new PlayerCollectData{id=7,count=50});
        player.SetCollectData(7,-3);Assert.AreEqual(-2,player.CollectRecords[0].count);
        Assert.AreEqual(50,player.CollectRecords[1].count);
        data.PlayerCollectDatas[0].count=int.MaxValue;player.SetCollectData(7,1);
        Assert.AreEqual(int.MinValue,player.CollectRecords[0].count);
        data.PlayerCollectDatas[0].isRecieve=true;player.SetCollectData(7,20);
        Assert.AreEqual(int.MinValue,player.CollectRecords[0].count);Assert.AreEqual(50,player.CollectRecords[1].count);
        Assert.AreEqual(4,saved.Count);
        var reloaded=JsonUtility.FromJson<PlayerData>(saved[3]);
        Assert.AreEqual(2,reloaded.RandomIndex);Assert.AreEqual(2,reloaded.PlayerCollectDatas.Count);
        Assert.IsTrue(reloaded.PlayerCollectDatas[0].isRecieve);
        Assert.AreEqual(int.MinValue,reloaded.PlayerCollectDatas[0].count);
        data.PlayerCollectDatas=null;player.SetCollectData(7,20);
        Assert.IsNull(player.CollectRecords);Assert.AreEqual(5,saved.Count);
    }
}
