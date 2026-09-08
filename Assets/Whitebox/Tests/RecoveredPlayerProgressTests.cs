using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredPlayerProgressTests
{
    private static RecoveredGameplayRules Rules() => new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Qonrii=new QonriiPoro {MojGping=new List<int>{10},Rgtigk=new List<int>{2},
            Lgtgl=new List<int>{1,2,3},NggpGpin=new List<int>{5,10,20}}
    });
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
