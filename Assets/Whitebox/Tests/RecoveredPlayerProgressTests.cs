using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredPlayerProgressTests
{
    [Test]
    public void CashWindowSelectionDiffersFromPromptAndSkipsOnlyCompletedSteps()
    {
        var data=new PlayerData {GreenCount=5000};
        data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,step=7,isCashout=true});
        var progress=new RecoveredPlayerProgress(CashRules(),()=>Assert.Fail("Window selection does not save"),data);
        Assert.AreEqual(0,progress.FindCashOutWindowSelection(out bool hide));Assert.IsFalse(hide);
        Assert.AreEqual(1,progress.PrepareCashOutPrompt(),"Main ignores every recorded id, regardless of step.");
        data.PlayerCashOutDatas[0].step=1000;
        Assert.AreEqual(1,progress.FindCashOutWindowSelection(out hide));Assert.IsFalse(hide);
        data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=1,step=1000});
        Assert.AreEqual(2,progress.FindCashOutWindowSelection(out hide));Assert.IsFalse(hide);
    }
    [Test]
    public void CashWindowHidesFrameAtRecordCountThresholdEvenWithDuplicateOrUnfinishedRecords()
    {
        var data=new PlayerData();var progress=new RecoveredPlayerProgress(CashRules(),()=>Assert.Fail("No save"),data);
        Assert.AreEqual(0,progress.FindCashOutWindowSelection(out bool hide));Assert.IsFalse(hide);
        for(int i=0;i<3;i++)data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,step=0});
        Assert.AreEqual(-1,progress.FindCashOutWindowSelection(out hide));Assert.IsTrue(hide);
        data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=99,step=1000});
        Assert.AreEqual(-1,progress.FindCashOutWindowSelection(out hide));Assert.IsTrue(hide);
    }
    [Test]
    public void CashWindowUsesFirstMatchingDuplicateAndDoesNotRepairEmptyConfiguration()
    {
        var data=new PlayerData();data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,step=1000});data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,step=0});
        var progress=new RecoveredPlayerProgress(CashRules(),()=>Assert.Fail("No save"),data);
        Assert.AreEqual(1,progress.FindCashOutWindowSelection(out bool hide));Assert.IsFalse(hide);
        data.PlayerCashOutDatas.Reverse();Assert.AreEqual(0,progress.FindCashOutWindowSelection(out hide));Assert.IsFalse(hide);
        var emptyRules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rgpggm=new RgpggmPoro{Qogt=new List<int>()}});
        progress=new RecoveredPlayerProgress(emptyRules,()=>Assert.Fail("No save"),new PlayerData());
        Assert.AreEqual(-1,progress.FindCashOutWindowSelection(out hide));Assert.IsTrue(hide);
    }
    [TestCase(1,8)]
    [TestCase(-10,-3)]
    [TestCase(0,7)]
    [TestCase(int.MaxValue,int.MinValue+6)]
    public void CashTasksUpdateEveryMatchingStepRegardlessOfTypeOrPaidStateThenSave(int amount,int expected)
    {
        var data=new PlayerData();
        data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,type=9,step=2,count=7,isCashout=true});
        data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=1,type=2,step=3,count=13});
        data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=2,type=0,step=2,count=7});
        int saves=0;var progress=new RecoveredPlayerProgress(CashRules(),()=>{
            saves++;Assert.AreEqual(expected,data.PlayerCashOutDatas[0].count);Assert.AreEqual(expected,data.PlayerCashOutDatas[2].count);
        },data);
        progress.RefreshCashOutTask(2,amount);Assert.AreEqual(13,data.PlayerCashOutDatas[1].count);Assert.AreEqual(1,saves);
        progress.RefreshCashOutTask(99,1);Assert.AreEqual(2,saves);
    }
    [Test]
    public void CashTasksSaveEvenForMissingOrEmptyRecordsWithoutCreatingAny()
    {
        var data=new PlayerData();int saves=0;var progress=new RecoveredPlayerProgress(CashRules(),()=>saves++,data);
        progress.RefreshCashOutTask(2,1);Assert.IsEmpty(data.PlayerCashOutDatas);Assert.AreEqual(1,saves);
        data.PlayerCashOutDatas=null;progress.RefreshCashOutTask(2,1);Assert.IsNull(data.PlayerCashOutDatas);Assert.AreEqual(2,saves);
    }
    private static RecoveredGameplayRules CashRules() => new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Rgpggm=new RgpggmPoro {Qogt=new List<int>{100,3000,5000}}
    });
    [TestCase(99.99f,-1)]
    [TestCase(100f,0)]
    [TestCase(10000f,0)]
    [TestCase(float.NaN,-1)]
    public void CoreCashPromptChecksFirstUnrecordedTierAndOnlyMarksItOnce(float balance,int expected)
    {
        var data=new PlayerData {GreenCount=balance};
        var progress=new RecoveredPlayerProgress(CashRules(),()=>Assert.Fail("Ordinary check must not save"),data);
        Assert.AreEqual(expected,progress.PrepareCashOutPrompt());
        CollectionAssert.AreEqual(expected<0?new int[0]:new[]{0},data.CashOutTipIndexs);
        Assert.AreEqual(-1,progress.PrepareCashOutPrompt(),"Do not search later tiers after an already shown first absent tier");
        Assert.AreEqual(0,data.PlayerCashOutDatas.Count);
    }
    [Test]
    public void CashPromptIgnoresRecordStatusAndDoesNotConsumeLaterTierBeforeItsThreshold()
    {
        var data=new PlayerData {GreenCount=2999};
        data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,isCashout=false,step=0});
        data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=2,isCashout=true});
        var progress=new RecoveredPlayerProgress(CashRules(),()=>Assert.Fail("No save"),data);
        Assert.AreEqual(-1,progress.PrepareCashOutPrompt());
        data.GreenCount=3000;
        Assert.AreEqual(1,progress.PrepareCashOutPrompt());
        CollectionAssert.AreEqual(new[]{1},data.CashOutTipIndexs);
        data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=1});
        Assert.AreEqual(-1,progress.PrepareCashOutPrompt());
    }
    [Test]
    public void MissingPromptListIsSavedBeforeEligibilityAndOnlyWhenAnAbsentTierExists()
    {
        var data=new PlayerData {GreenCount=100,CashOutTipIndexs=null};int saves=0;
        var progress=new RecoveredPlayerProgress(CashRules(),()=>{
            saves++;Assert.IsNotNull(data.CashOutTipIndexs);Assert.IsEmpty(data.CashOutTipIndexs);
        },data);
        Assert.AreEqual(0,progress.PrepareCashOutPrompt());Assert.AreEqual(1,saves);
        Assert.AreEqual(-1,progress.PrepareCashOutPrompt());Assert.AreEqual(1,saves);
        data.CashOutTipIndexs=null;data.GreenCount=0;
        Assert.AreEqual(-1,progress.PrepareCashOutPrompt());Assert.AreEqual(2,saves);
        for(int i=0;i<3;i++)data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=i});
        data.CashOutTipIndexs=null;
        Assert.AreEqual(-1,progress.PrepareCashOutPrompt());Assert.IsNull(data.CashOutTipIndexs);Assert.AreEqual(2,saves);
    }
    private static RecoveredGameplayRules Rules() => new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Qonrii=new QonriiPoro {MojGping=new List<int>{10},Rgtigk=new List<int>{2},
            Lgtgl=new List<int>{1,2,3},NggpGpin=new List<int>{5,10,20}}
    });
    [TestCase(19999f,0,0)]
    [TestCase(20000f,0,1)]
    [TestCase(39999f,1,1)]
    [TestCase(40000f,1,2)]
    [TestCase(60000f,2,3)]
    [TestCase(80000f,3,4)]
    [TestCase(100000f,0,1)]
    [TestCase(-5f,0,0)]
    public void GreenBalanceNotifiesOldStateThenPersistsTwice(float requested,int milestone,int expectedMilestone)
    {
        var data=new PlayerData {GreenCount=17,GreenCountLog=milestone};
        var order=new List<string>();
        var progress=new RecoveredPlayerProgress(Rules(),()=>{
            Assert.AreEqual(requested,data.GreenCount);
            Assert.AreEqual(expectedMilestone,data.GreenCountLog);
            order.Add("save");
        },data);
        progress.GreenCountChanged+=(before,after)=>{
            Assert.AreEqual(17,before); Assert.AreEqual(requested,after);
            Assert.AreEqual(17,progress.GreenCount); Assert.AreEqual(milestone,data.GreenCountLog);
            order.Add("event");
        };
        progress.SetGreenCount(requested);
        CollectionAssert.AreEqual(new[]{"event","save","save"},order);
    }

    [Test]
    public void GreenMilestonesAdvanceOncePerAssignmentEvenWhenBalanceDoesNotChange()
    {
        var data=new PlayerData(); int saves=0;
        var progress=new RecoveredPlayerProgress(Rules(),()=>saves++,data);
        for(int i=1;i<=4;i++) {progress.SetGreenCount(100000); Assert.AreEqual(i,data.GreenCountLog);}
        progress.SetGreenCount(100000);
        Assert.AreEqual(4,data.GreenCountLog); Assert.AreEqual(10,saves);
    }

    [Test]
    public void GreenEventFailurePreventsMutationAndNaNFollowsNativeUnorderedBranch()
    {
        var data=new PlayerData {GreenCount=17}; int saves=0;
        var progress=new RecoveredPlayerProgress(Rules(),()=>saves++,data);
        System.Action<float,float> fail=(before,after)=>throw new System.InvalidOperationException();
        progress.GreenCountChanged+=fail;
        Assert.Throws<System.InvalidOperationException>(()=>progress.SetGreenCount(20000));
        Assert.AreEqual(17,data.GreenCount); Assert.AreEqual(0,saves); Assert.AreEqual(0,data.GreenCountLog);
        progress.GreenCountChanged-=fail;
        progress.SetGreenCount(float.NaN);
        Assert.IsTrue(float.IsNaN(data.GreenCount)); Assert.AreEqual(1,data.GreenCountLog); Assert.AreEqual(2,saves);
    }
    [TestCase(0,1,1)]
    [TestCase(1,2,1)]
    [TestCase(2,2,0)]
    [TestCase(5,5,0)]
    [TestCase(-2,-1,1)]
    public void BonusAreaOnlySavesAcceptedHitsAndPreservesOtherColumns(int before, int after, int expectedSaves)
    {
        var data = new PlayerData {BonusArea=new List<int>{7,0,before,1,2}};
        var original = data.BonusArea; int saves = 0;
        var progress = new RecoveredPlayerProgress(Rules(), () => {
            saves++;
            CollectionAssert.AreEqual(new[]{7,0,after,1,2}, data.BonusArea);
        }, data);
        progress.SetBonusArea(2);
        Assert.AreEqual(expectedSaves, saves);
        Assert.AreSame(original, data.BonusArea);
        Assert.AreEqual(after, data.BonusArea[2]);
        Assert.IsFalse(progress.IsBonusGame);
    }

    [TestCase(-1)]
    [TestCase(5)]
    public void InvalidBonusColumnFailsWithoutSaving(int reel)
    {
        var data = new PlayerData(); int saves = 0;
        var progress = new RecoveredPlayerProgress(Rules(), () => saves++, data);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => progress.SetBonusArea(reel));
        Assert.AreEqual(0, saves);
        CollectionAssert.AreEqual(new[]{0,0,0,0,0}, data.BonusArea);
    }

    [Test]
    public void LastBonusHitFeedsEntryThenResetsProgress()
    {
        var data = new PlayerData {BonusArea=new List<int>{2,2,2,2,1}};
        int saves = 0; var rules = Rules();
        var progress = new RecoveredPlayerProgress(rules, () => saves++, data);
        var branches = new RecoveredRewardBranches(rules, progress);
        Assert.IsFalse(branches.CheckBonusGame());
        progress.SetBonusArea(4);
        Assert.AreEqual(1, saves);
        Assert.IsTrue(branches.CheckBonusGame());
        Assert.AreEqual(3, saves);
        Assert.AreEqual(5, data.PlayerTaskDatas[0].id);
        CollectionAssert.AreEqual(new[]{0,0,0,0,0}, data.BonusArea);
        Assert.IsFalse(branches.CheckBonusGame());
    }
    [TestCase(20,10)] [TestCase(-3,0)] [TestCase(4,4)]
    public void SpinEventFollowsSaveAndUsesRawRequest(int requested,int stored)
    {
        var events=new List<string>();
        var progress=new RecoveredPlayerProgress(Rules(),()=>events.Add("save"),1,0,3,0);
        progress.SpinCountChanged+=value=>{Assert.AreEqual(requested,value); events.Add("event");};
        progress.SetSpinCount(requested);
        Assert.AreEqual(stored,progress.SpinCount);
        CollectionAssert.AreEqual(new[]{"save","event"},events);
    }
    [Test]
    public void LevelUpDiscardsOverflowAndNotifiesThresholdBeforeSave()
    {
        var events=new List<string>();
        var progress=new RecoveredPlayerProgress(Rules(),()=>events.Add("save"),1,4,3,0);
        progress.ReviewRequested+=()=>events.Add("review");
        progress.LevelExperienceChanged+=(level,value,max)=>{
            Assert.AreEqual(2,level); Assert.AreEqual(5,value); Assert.AreEqual(5,max);
            Assert.AreEqual(0,progress.Experience); events.Add("experience");
        };
        progress.SetExperience(1000);
        Assert.AreEqual(2,progress.Level);
        CollectionAssert.AreEqual(new[]{"review","experience","save"},events);
    }
    [Test]
    public void OrdinaryExperienceAndNegativeMoreWildAreNotClamped()
    {
        var events=new List<string>();
        var progress=new RecoveredPlayerProgress(Rules(),()=>events.Add("save"),1,0,3,0);
        progress.LevelExperienceChanged+=(level,value,max)=>{
            Assert.AreEqual(-2,value); Assert.AreEqual(5,max); events.Add("experience");
        };
        progress.MoreWildChanged+=()=>events.Add("wild");
        progress.SetExperience(-2); progress.SetMoreWild(-1);
        Assert.AreEqual(-2,progress.Experience); Assert.AreEqual(-1,progress.MoreWild);
        CollectionAssert.AreEqual(new[]{"experience","save","wild","save"},events);
    }
    [Test]
    public void ExperienceRequirementUsesLastTierAtConfiguredLastLevel()
    {
        var rules=Rules();
        Assert.AreEqual(5,rules.GetNeedPro(1)); Assert.AreEqual(10,rules.GetNeedPro(2));
        Assert.AreEqual(20,rules.GetNeedPro(3)); Assert.AreEqual(20,rules.GetNeedPro(99));
    }
}
