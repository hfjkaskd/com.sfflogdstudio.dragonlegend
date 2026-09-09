using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredCashOutConditionsTests
{
    private static RecoveredPlayerProgress Model(PlayerData data)
    {
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
            Rgpggm=new RgpggmPoro {Qogt=new List<int>{100},Rogk1tir=new List<int>{10},Rogk1roil=new List<int>{20},Rogk6tir=new List<int>{10},Rogk6roil=new List<int>{20},Rimgg1=new List<int>{7},Rimg7=new List<int>{7}},
            Qollgqr=new QollgqrPoro {Ip=new List<int>{0,1,2},Lgtgl=new List<int>{1,1,1},Ronpom=new List<int>{1,1,1},Korrt=new List<int>{1,1,1}}
        });
        return new RecoveredPlayerProgress(rules,()=>Assert.Fail("Reading bottom conditions must not save"),data);
    }
    [TestCase(99.99f,false)]
    [TestCase(100f,true)]
    [TestCase(150f,true)]
    [TestCase(float.NaN,false)]
    public void UnrecordedTierUsesLiveBalanceWithoutClaimingTasksAreComplete(float balance,bool show)
    {
        var data=new PlayerData{GreenCount=balance};var state=Model(data).GetCashOutConditions(0,100);
        Assert.IsFalse(state.HasRecord);Assert.IsFalse(state.RequiresOrderStatus);Assert.AreEqual(show,state.ShowActionRow);
        Assert.IsFalse(state.TaskComplete);Assert.IsFalse(state.WaitComplete);Assert.IsEmpty(data.PlayerCashOutDatas);
        if(!float.IsNaN(balance))Assert.That(state.MissingCash,Is.EqualTo(100-balance).Within(.001f));
    }
    [TestCase(19,106,false,false)]
    [TestCase(20,106,true,false)]
    [TestCase(19,107,false,true)]
    [TestCase(20,107,true,true)]
    [TestCase(27,108,true,true)]
    public void TaskAndWaitAreIndependentAndDoNotHideTheActionRow(int count,int now,bool task,bool wait)
    {
        var data=new PlayerData();data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,step=0,count=count,time=100});
        var state=Model(data).GetCashOutConditions(0,now);
        Assert.IsTrue(state.HasRecord);Assert.IsTrue(state.ShowActionRow);Assert.AreEqual(task,state.TaskComplete);
        Assert.AreEqual(wait,state.WaitComplete);Assert.AreEqual(count,state.TaskCount);Assert.AreEqual(20,state.TaskGoal);
        Assert.AreEqual(107-now,state.RemainingSeconds);Assert.AreEqual(0,data.GreenCount);
    }
    [Test]
    public void UsesFirstRecordSuccessConfigAndRawCollectRecordCount()
    {
        var data=new PlayerData();var record=new PlayerCashOutData{id=0,type=99,step=0,count=10,time=100,isCashout=true};
        data.PlayerCashOutDatas.Add(record);data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,step=1000});var model=Model(data);
        var state=model.GetCashOutConditions(0,100);Assert.IsTrue(state.TaskComplete);Assert.AreEqual(10,state.TaskGoal);
        record.step=6;record.count=999;data.PlayerCollectDatas.Add(new PlayerCollectData{id=0,count=999});data.PlayerCollectDatas.Add(new PlayerCollectData{id=0,count=999});
        state=model.GetCashOutConditions(0,100);Assert.AreEqual(2,state.TaskCount);Assert.AreEqual(3,state.TaskGoal);Assert.IsFalse(state.TaskComplete);
        data.PlayerCollectDatas.Add(new PlayerCollectData{id=0,count=0});Assert.IsTrue(model.GetCashOutConditions(0,100).TaskComplete);
        data.PlayerCashOutDatas.Reverse();state=model.GetCashOutConditions(0,100);
        Assert.IsTrue(state.RequiresOrderStatus);Assert.IsFalse(state.ShowActionRow);Assert.IsFalse(state.TaskComplete);Assert.IsFalse(state.WaitComplete);
    }
    [Test]
    public void TimestampArithmeticRetainsNativeIntOverflow()
    {
        var data=new PlayerData();data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,step=0,time=int.MaxValue});
        var state=Model(data).GetCashOutConditions(0,0);
        Assert.AreEqual(unchecked(int.MaxValue+7),state.RemainingSeconds);Assert.IsTrue(state.WaitComplete);
    }
}
