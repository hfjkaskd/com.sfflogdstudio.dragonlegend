using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredBonusRoundTests
{
    private static RecoveredGameplayRules Rules(int zhao, int cai, int jin, int bao, int reward)
        => new RecoveredGameplayRules(new GoldenDragonAutoGenConfig { Ronig=new RonigPoro {
            Ltoo=new List<int>{zhao}, Qoi=new List<int>{cai}, Jin=new List<int>{jin},
            Roo=new List<int>{bao}, Rgkorp=new List<int>{reward} } });

    private static RecoveredBonusRound.Reveal Pick(RecoveredBonusRound round, RecoveredBonusType type)
    {
        for (int i=0; i<round.Count; i++)
            if (!round.WasClicked(i) && round.GetCard(i)==type) {
                Assert.IsTrue(round.TryReveal(i, out var result)); return result;
            }
        Assert.Fail("No unselected card of requested type."); return default;
    }

    [Test]
    public void OverlappingTypesConsumeGrandFirstThenOneOrderedTargetAtATime()
    {
        var state=Random.state;
        try {
            Random.InitState(301);
            var round=new RecoveredBonusRound(); var rules=Rules(1,5,1,5,1);
            round.Initialize(rules); Assert.AreEqual(13, round.Count);
            var afterShuffle=Random.state;
            var cai=Pick(round,RecoveredBonusType.Cai);
            Assert.AreEqual(RecoveredJackpotType.Grand,cai.jackpot);
            Assert.AreEqual(1,cai.targetIndex); Assert.IsFalse(cai.completesJackpot);
            var bao=Pick(round,RecoveredBonusType.Bao);
            Assert.AreEqual(RecoveredJackpotType.Grand,bao.jackpot); Assert.AreEqual(3,bao.targetIndex);
            for (int i=0;i<3;i++) {
                cai=Pick(round,RecoveredBonusType.Cai); bao=Pick(round,RecoveredBonusType.Bao);
                Assert.AreEqual(RecoveredJackpotType.Minor,cai.jackpot);
                Assert.AreEqual(RecoveredJackpotType.Major,bao.jackpot);
                Assert.AreEqual(i,cai.targetIndex); Assert.AreEqual(i,bao.targetIndex);
                Assert.AreEqual(i==2,cai.completesJackpot); Assert.AreEqual(i==2,bao.completesJackpot);
            }
            Assert.AreEqual(RecoveredJackpotType.None,Pick(round,RecoveredBonusType.Cai).jackpot);
            Assert.AreEqual(RecoveredJackpotType.None,Pick(round,RecoveredBonusType.Bao).jackpot);
            Assert.IsFalse(Pick(round,RecoveredBonusType.Zhao).completesJackpot);
            Assert.IsTrue(Pick(round,RecoveredBonusType.Jin).completesJackpot);
            var cash=Pick(round,RecoveredBonusType.Reward);
            Assert.AreEqual(RecoveredJackpotType.None,cash.jackpot);
            Assert.AreEqual(-1,cash.targetIndex); Assert.IsFalse(cash.completesJackpot);
            Assert.AreEqual(13,round.ClickedCount);
            Assert.AreEqual(afterShuffle,Random.state,"Reveal must not roll ordinary cash before its animation.");
            for (int i=0;i<round.Count;i++) Assert.IsFalse(round.TryReveal(i,out _));
            Assert.AreEqual(13,round.ClickedCount);
            round.Initialize(rules); Assert.AreEqual(0,round.ClickedCount);
            Assert.AreEqual(RecoveredJackpotType.Grand,Pick(round,RecoveredBonusType.Cai).jackpot);
        } finally { Random.state=state; }
    }

    [UnityTest]
    public IEnumerator RealProfilesKeepCorrelatedRowsAndExactCardMultiplicities()
    {
        var state=Random.state;
        try {
            foreach (var path in new[]{"Bundled/GoldenDragon.json","Bundled/GoldenDragon_default.json",
                "Bundled/GoldenDragon_organic.json","Remote/cp_default.json","Remote/cp_default_1.json","Remote/cp_test.json"}) {
                var loader=new ConfigSnapshotLoader(); yield return loader.Load("RecoveredConfig/"+path);
                Assert.IsNotNull(loader.Value); var c=loader.Value.Ronig;
                var rules=new RecoveredGameplayRules(loader.Value);
                Assert.AreEqual(c.RrggRimgg[0],rules.GetBonusFreeTimes());
                var round=new RecoveredBonusRound();
                for (int seed=0;seed<24;seed++) {
                    Random.InitState(seed); int row=Random.Range(0,c.Ltoo.Count);
                    var expected=new[]{c.Rgkorp[row],c.Ltoo[row],c.Qoi[row],c.Jin[row],c.Roo[row]};
                    Random.InitState(seed); var counts=rules.GetBonusAllReward();
                    CollectionAssert.AreEqual(new[]{expected[1],expected[2],expected[3],expected[4],expected[0]},counts);
                    Assert.AreSame(counts,rules.GetBonusAllReward());
                    Random.InitState(seed); round.Initialize(rules);
                    var actual=new int[5];
                    for(int i=0;i<round.Count;i++) actual[(int)round.GetCard(i)]++;
                    CollectionAssert.AreEqual(expected,actual,path+" seed "+seed);
                    // The native shuffle makes one Range(i,n) call per card, including the last.
                    var observed=Random.state;
                    Random.InitState(seed); Random.Range(0,c.Ltoo.Count);
                    for(int i=0;i<round.Count;i++) Random.Range(i,round.Count);
                    Assert.AreEqual(Random.state,observed);
                }
            }
        } finally { Random.state=state; }
    }

    [Test]
    public void CashRangeIsInclusiveAndJumpUsesAnyNonzeroValue()
    {
        var state=Random.state;
        try {
            var config=new GoldenDragonAutoGenConfig { Ronig=new RonigPoro {
                RgkorpKgiitr=new List<int>{1}, RgokrpMin=new List<int>{40},
                RgkorpMoj=new List<int>{41}, Jimp=new List<int>{-3} } };
            var rules=new RecoveredGameplayRules(config); bool minimum=false,maximum=false;
            for(int seed=0;seed<100;seed++) {
                Random.InitState(seed); var result=rules.GetBonusReward();
                Assert.IsTrue(result.jump); Assert.That(result.amount,Is.InRange(40,41));
                minimum|=result.amount==40; maximum|=result.amount==41;
            }
            Assert.IsTrue(minimum && maximum);
            config.Ronig.Jimp[0]=0; Assert.IsFalse(rules.GetBonusReward().jump);
        } finally { Random.state=state; }
    }
}
