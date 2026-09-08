using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredRewardBranchesTests
{
    private static RecoveredGameplayRules Rules() => new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Rogk=new RogkPoro {Ip=new List<int>{2,3},RogkOmoinr=new List<int>{5,4},
            Rgkorp=new List<int>{10,20},Jimp=new List<int>{1,2}},
        Rrggiomg=new RrggiomgPoro {RrggGping=new List<int>{0,0,7,0,9,12}}
    });

    [Test]
    public void FlyCoinCreditsLatestBalanceAfterCallerAndTopTitleReset()
    {
        var data=new PlayerData {GreenCount=10}; var order=new List<string>();
        var rules=Rules();
        var progress=new RecoveredPlayerProgress(rules,()=>order.Add("save"),data);
        progress.GreenCountChanged+=(oldValue,newValue)=>{
            Assert.AreEqual(30,oldValue); Assert.AreEqual(35,newValue);
            order.Add("balance");
        };
        var branches=new RecoveredRewardBranches(rules,progress);
        branches.CompleteFlyCoin(5,()=>{Assert.AreEqual(10,progress.GreenCount); data.GreenCount=20;order.Add("caller");},
            ()=>{Assert.AreEqual(20,progress.GreenCount);data.GreenCount=30;order.Add("top");});
        CollectionAssert.AreEqual(new[]{"caller","top","balance","save","save"},order);
        Assert.AreEqual(35,progress.GreenCount);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void FlyCoinCallbackFailurePreventsCredit(bool failCaller)
    {
        var data=new PlayerData {GreenCount=10};int saves=0,resets=0;
        var rules=Rules();var progress=new RecoveredPlayerProgress(rules,()=>saves++,data);
        var branches=new RecoveredRewardBranches(rules,progress);
        Assert.Throws<InvalidOperationException>(()=>branches.CompleteFlyCoin(5,
            ()=>{if(failCaller)throw new InvalidOperationException();},
            ()=>{resets++;throw new InvalidOperationException();}));
        Assert.AreEqual(failCaller?0:1,resets);Assert.AreEqual(10,progress.GreenCount);Assert.AreEqual(0,saves);
    }

    [TestCase(5f,20f)]
    [TestCase(-8f,-6f)]
    public void FlyCoinAllowsNullCallerAndRepeatedCompletionRetainsNativeCredits(float amount,float expected)
    {
        var data=new PlayerData {GreenCount=10};int saves=0,resets=0;
        var rules=Rules();var progress=new RecoveredPlayerProgress(rules,()=>saves++,data);
        var branches=new RecoveredRewardBranches(rules,progress);
        branches.CompleteFlyCoin(amount,null,()=>resets++);
        branches.CompleteFlyCoin(amount,null,()=>resets++);
        Assert.AreEqual(expected,progress.GreenCount);Assert.AreEqual(2,resets);Assert.AreEqual(4,saves);
    }
    [TestCase(2,RecoveredJackpotType.None,0)]
    [TestCase(3,RecoveredJackpotType.Minor,1)]
    [TestCase(4,RecoveredJackpotType.Major,1)]
    [TestCase(5,RecoveredJackpotType.Grand,1)]
    public void JackpotTierAndTaskFollowCompleteColumnCount(int columns,RecoveredJackpotType tier,int savesExpected)
    {
        var data=new PlayerData(); int saves=0; var rules=Rules();
        var progress=new RecoveredPlayerProgress(rules,()=>saves++,data);
        var branches=new RecoveredRewardBranches(rules,progress);
        Assert.AreEqual(tier,branches.CheckJackpot(columns));
        Assert.AreEqual(savesExpected,saves);
        Assert.AreEqual(savesExpected,data.PlayerTaskDatas.Count);
        if (savesExpected > 0) {Assert.AreEqual(2,data.PlayerTaskDatas[0].id); Assert.AreEqual(1,data.PlayerTaskDatas[0].count);}
    }

    [Test]
    public void FreeGameUsesConfigurationInsteadOfHardcodedScatterThreshold()
    {
        var data=new PlayerData(); int saves=0; var rules=Rules();
        var branches=new RecoveredRewardBranches(rules,new RecoveredPlayerProgress(rules,()=>saves++,data));
        Assert.AreEqual(0,branches.CheckFreeGame(3));
        Assert.AreEqual(0,saves);
        Assert.AreEqual(7,branches.CheckFreeGame(2));
        Assert.AreEqual(1,saves);
        Assert.AreEqual(3,data.PlayerTaskDatas[0].id);
    }

    [Test]
    public void NewTaskIgnoresIncrementThenExistingTaskClampsAndClaimedTaskOnlySaves()
    {
        var data=new PlayerData(); int saves=0;
        var progress=new RecoveredPlayerProgress(Rules(),()=>saves++,data);
        progress.SetTaskData(2,99); Assert.AreEqual(1,data.PlayerTaskDatas[0].count);
        progress.SetTaskData(2,99); Assert.AreEqual(5,data.PlayerTaskDatas[0].count);
        progress.SetTaskData(2,-99); Assert.AreEqual(0,data.PlayerTaskDatas[0].count);
        data.PlayerTaskDatas[0].isRecieve=true;
        progress.SetTaskData(2,99); Assert.AreEqual(0,data.PlayerTaskDatas[0].count);
        Assert.AreEqual(4,saves);
        data.PlayerTaskDatas=null;
        progress.SetTaskData(3,1); Assert.IsNull(data.PlayerTaskDatas); Assert.AreEqual(5,saves);
    }

    [Test]
    public void UnknownExistingTaskFailsAfterIncrementBeforeSave()
    {
        var data=new PlayerData(); int saves=0;
        var progress=new RecoveredPlayerProgress(Rules(),()=>saves++,data);
        progress.SetTaskData(99,-8); Assert.AreEqual(1,data.PlayerTaskDatas[0].count);
        Assert.Throws<NullReferenceException>(()=>progress.SetTaskData(99,2));
        Assert.AreEqual(3,data.PlayerTaskDatas[0].count);
        Assert.AreEqual(1,saves);
    }

    [Test]
    public void FreeEntrySavesTaskAndRefreshesBeforeReplacingRuntimeState()
    {
        var data = new PlayerData(); var order = new List<string>();
        var rules = Rules(); RecoveredPlayerProgress progress = null;
        progress = new RecoveredPlayerProgress(rules, () => {
            Assert.AreEqual(3, data.PlayerTaskDatas[0].id);
            Assert.AreEqual(RecoveredSlotType.Base, progress.GameSlotType);
            Assert.AreEqual(99, progress.FreeSpinCount);
            Assert.AreEqual(123, progress.TotalFreeSpinWin);
            order.Add("save");
        }, data) {FreeSpinCount=99,TotalFreeSpinWin=123};
        var branches = new RecoveredRewardBranches(rules,progress);
        branches.CashOutTaskRefreshRequested += (type, amount) => {
            CollectionAssert.AreEqual(new[]{"save"}, order);
            Assert.AreEqual(5,type); Assert.AreEqual(1,amount);
            Assert.AreEqual(RecoveredSlotType.Base,progress.GameSlotType);
            Assert.AreEqual(99,progress.FreeSpinCount);
            Assert.AreEqual(123,progress.TotalFreeSpinWin);
            order.Add("refresh");
        };
        Assert.AreEqual(9,branches.CheckFreeGame(4));
        CollectionAssert.AreEqual(new[]{"save","refresh"}, order);
        Assert.AreEqual(RecoveredSlotType.Free,progress.GameSlotType);
        Assert.AreEqual(9,progress.FreeSpinCount);
        Assert.AreEqual(0,progress.TotalFreeSpinWin);
    }

    [Test]
    public void NonTriggeringFreeCheckLeavesActiveRuntimeStateUntouched()
    {
        var rules = Rules(); int calls = 0;
        var progress = new RecoveredPlayerProgress(rules,()=>calls++,new PlayerData()) {
            GameSlotType=RecoveredSlotType.Free,FreeSpinCount=4,TotalFreeSpinWin=35};
        var branches = new RecoveredRewardBranches(rules,progress);
        branches.CashOutTaskRefreshRequested += (type,amount)=>calls++;
        Assert.AreEqual(0,branches.CheckFreeGame(3));
        Assert.AreEqual(0,calls);
        Assert.AreEqual(RecoveredSlotType.Free,progress.GameSlotType);
        Assert.AreEqual(4,progress.FreeSpinCount);
        Assert.AreEqual(35,progress.TotalFreeSpinWin);
    }
    [TestCase(1, false, false)]
    [TestCase(2, false, true)]
    [TestCase(3, false, true)]
    [TestCase(0, true, true)]
    public void BonusEntryUsesAreaThresholdOrPendingFlag(int lastArea, bool pending, bool enters)
    {
        var original = new List<int> {2,2,2,2,lastArea};
        var data = new PlayerData {BonusArea=original};
        int saves = 0;
        RecoveredPlayerProgress progress = null;
        progress = new RecoveredPlayerProgress(Rules(), () => {
            saves++;
            Assert.AreEqual(5, data.PlayerTaskDatas[0].id);
            if (saves == 1) {
                Assert.AreSame(original, data.BonusArea);
                Assert.IsTrue(progress.IsBonusGame);
            } else {
                Assert.AreNotSame(original, data.BonusArea);
                CollectionAssert.AreEqual(new[] {0,0,0,0,0}, data.BonusArea);
                Assert.IsFalse(progress.IsBonusGame);
            }
        }, data) {IsBonusGame=pending};
        var branches = new RecoveredRewardBranches(Rules(), progress);
        Assert.AreEqual(enters, branches.CheckBonusGame());
        Assert.AreEqual(enters ? 2 : 0, saves);
        Assert.AreEqual(lastArea, original[4]);
        Assert.IsFalse(progress.IsBonusGame);
        Assert.IsFalse(branches.CheckBonusGame());
        Assert.AreEqual(enters ? 2 : 0, saves);
    }

    [Test]
    public void EmptyBonusAreaPreservesOriginalCountEquality()
    {
        var data = new PlayerData {BonusArea=new List<int>()};
        int saves = 0; var rules = Rules();
        var progress = new RecoveredPlayerProgress(rules, () => saves++, data);
        Assert.IsTrue(new RecoveredRewardBranches(rules, progress).CheckBonusGame());
        Assert.AreEqual(2, saves);
        CollectionAssert.AreEqual(new[] {0,0,0,0,0}, data.BonusArea);
    }

    [Test]
    public void NullBonusAreaFailsBeforePendingFlagIsConsumedOrDataSaved()
    {
        var data = new PlayerData {BonusArea=null};
        int saves = 0; var rules = Rules();
        var progress = new RecoveredPlayerProgress(rules, () => saves++, data) {IsBonusGame=true};
        Assert.Throws<NullReferenceException>(() => new RecoveredRewardBranches(rules, progress).CheckBonusGame());
        Assert.AreEqual(0, saves);
        Assert.IsTrue(progress.IsBonusGame);
    }
    private sealed class ColumnOrder : Random
    {
        private readonly int[] keys={2,0,4,1,3}; private int index;
        public override int Next() => keys[index++];
    }

    [Test]
    public void NonConsecutiveWildColumnsStillCountTowardJackpot()
    {
        var weights=new List<int>{100};
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {Gimrol=new GimrolPoro {
            Rggl1=weights,Rggl2=weights,Rggl3=weights,Rggl4=weights,Rggl5=weights}});
        var state=UnityEngine.Random.state;
        try
        {
            var board=new RecoveredSlotBoard(rules); board.FillBase();
            board.ApplyGuaranteedWildIndex(1,new ColumnOrder());
            Assert.AreEqual(7,board.GetSymbol(0,0)); Assert.AreEqual(7,board.GetSymbol(1,0));
            Assert.AreEqual(0,board.GetSymbol(2,0)); Assert.AreEqual(7,board.GetSymbol(3,0));
            Assert.AreEqual(3,RecoveredRewardBranches.CountWildColumns(board));
        }
        finally {UnityEngine.Random.state=state;}
    }
}
