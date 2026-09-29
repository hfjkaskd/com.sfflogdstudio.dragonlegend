using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

public sealed class MoreWildBoostTests
{
    private const string HybridPath = "RecoveredConfig/Remote/cp_low_frequency_high_rewards.json";

    private sealed class View : IRecoveredMoreWildView
    {
        public void PlaySound(string sound) { }
        public void HideFinger() { }
        public void Hide() { }
    }

    [UnityTest]
    public IEnumerator EveryAdvertisedChargeBoostsOneAcceptedSpinIncludingTheLast()
    {
        var loader = new ConfigSnapshotLoader();
        yield return loader.Load(HybridPath);
        var previousRandom = Random.state;
        try
        {
            // Also exercise a four-charge configuration: the claim's advertised
            // count and the actual duration must stay aligned without a magic 5.
            foreach (int advertisedCount in new[] { 5, 4 })
            {
                Random.InitState(7113);
                var config = IsolatedConfig(loader.Value, advertisedCount);
                var rules = new RecoveredGameplayRules(config);
                var data = new PlayerData { GuideStep = 2, SpinCount = 10 };
                var progress = new RecoveredPlayerProgress(rules, () => { }, data);
                var result = new RecoveredSpinResult(rules, new RecoveredSlotSettlement(rules));
                var entry = new RecoveredSpinEntry(rules, data, progress, result, () => { });
                var ads = new LocalAdFacade();
                var claim = new RecoveredMoreWildClaim(rules, progress, data, ads, new View());

                Assert.AreEqual(advertisedCount, claim.BeforeShow(true), "Popup amount must match the grant.");
                claim.Click("ClaimBtn");
                Assert.AreEqual(advertisedCount, progress.MoreWild);
                Assert.AreEqual(3, data.GuideStep, "The ordinary spin path must not use the first-spin guarantee.");
                Assert.IsFalse(ads.Pending);
                int changes = 0;
                progress.MoreWildChanged += () => changes++;

                for (int spin = 0; spin < advertisedCount; spin++)
                {
                    Assert.AreEqual(advertisedCount - spin, progress.MoreWild, "Remaining amount before acceptance.");
                    Assert.IsTrue(entry.TryBegin(false, 1, "default", 100));
                    Assert.AreEqual(advertisedCount - spin - 1, progress.MoreWild, "Exactly one charge is spent.");
                    Assert.AreEqual(15, CountWilds(result.Board),
                        "Accepted spin " + (spin + 1) + " of " + advertisedCount + " must select the enhanced table.");
                    Assert.IsFalse(result.FirstFreeReward);
                    Finish(result);
                }

                Assert.AreEqual(advertisedCount, changes);
                Assert.AreEqual(0, progress.MoreWild);
                Assert.IsTrue(entry.TryBegin(false, 1, "default", 100));
                Assert.AreEqual(0, CountWilds(result.Board), "The next accepted spin must return to the ordinary table.");
                Assert.AreEqual(0, progress.MoreWild);
                Assert.AreEqual(advertisedCount, changes, "An ordinary spin must not consume another boost charge.");
                Assert.AreEqual(10 - advertisedCount - 1, progress.SpinCount);
                Finish(result);
            }
        }
        finally { Random.state = previousRandom; }
    }

    [UnityTest]
    public IEnumerator BusyEmptyAndAlreadyGeneratingRequestsDoNotConsumeBoostCharges()
    {
        var loader = new ConfigSnapshotLoader();
        yield return loader.Load(HybridPath);
        var previousRandom = Random.state;
        try
        {
            Random.InitState(7113);
            var rules = new RecoveredGameplayRules(IsolatedConfig(loader.Value, 5));
            var data = new PlayerData { GuideStep = 3, SpinCount = 10, MoreWild = 5 };
            int saves = 0, changes = 0, moreSpins = 0;
            System.Action save = () => saves++;
            var progress = new RecoveredPlayerProgress(rules, save, data);
            progress.MoreWildChanged += () => changes++;
            var result = new RecoveredSpinResult(rules, new RecoveredSlotSettlement(rules));
            var entry = new RecoveredSpinEntry(rules, data, progress, result, save);
            entry.MoreSpinsRequested += () => moreSpins++;

            string before = JsonUtility.ToJson(data);
            Assert.IsFalse(entry.TryBegin(true, 1, "default", 100));
            Assert.AreEqual(before, JsonUtility.ToJson(data), "Presenter busy rejection must not mutate the player.");
            Assert.AreEqual(0, saves);
            Assert.AreEqual(0, changes);
            Assert.AreEqual(0, moreSpins);

            data.SpinCount = 0;
            before = JsonUtility.ToJson(data);
            Assert.IsFalse(entry.TryBegin(false, 1, "default", 100));
            Assert.AreEqual(before, JsonUtility.ToJson(data), "No-spin-stock rejection must not mutate the player.");
            Assert.AreEqual(0, saves);
            Assert.AreEqual(0, changes);
            Assert.AreEqual(1, moreSpins);

            data.SpinCount = 10;
            Assert.IsTrue(entry.TryBegin(false, 1, "default", 100));
            Assert.AreEqual(4, progress.MoreWild);
            Assert.IsTrue(result.IsGenerating);
            before = JsonUtility.ToJson(data);
            int savedCount = saves;
            Assert.IsFalse(entry.TryBegin(false, 1, "default", 100));
            Assert.AreEqual(before, JsonUtility.ToJson(data), "Result-generation rejection must not mutate the player.");
            Assert.AreEqual(savedCount, saves);
            Assert.AreEqual(1, changes);
            Assert.AreEqual(15, CountWilds(result.Board));
            Finish(result);
        }
        finally { Random.state = previousRandom; }
    }

    [UnityTest]
    public IEnumerator ShippedHybridKeepsOrdinaryTablesAndOnlyUsesHighWildTablesDuringBoost()
    {
        var hybridLoader = new ConfigSnapshotLoader();
        var ordinaryLoader = new ConfigSnapshotLoader();
        var highLoader = new ConfigSnapshotLoader();
        yield return hybridLoader.Load(HybridPath);
        yield return ordinaryLoader.Load("RecoveredConfig/Remote/cp_default_1.json");
        yield return highLoader.Load("RecoveredConfig/Remote/cp_test.json");
        var hybrid = new RecoveredGameplayRules(hybridLoader.Value);
        var ordinary = new RecoveredGameplayRules(ordinaryLoader.Value);
        var high = new RecoveredGameplayRules(highLoader.Value);
        Assert.AreEqual(5, hybrid.GetMoreWild());
        Assert.AreEqual(ordinary.GetWildSpinCD(), hybrid.GetWildSpinCD());
        for (int column = 0; column < 5; column++)
        {
            CollectionAssert.AreEqual(ordinary.ReelWeights(column, 0), hybrid.ReelWeights(column, 0), "Base symbols changed.");
            CollectionAssert.AreEqual(ordinary.ReelWeights(column, 1), hybrid.ReelWeights(column, 1), "Ordinary WILD frequency changed.");
            CollectionAssert.AreEqual(high.ReelWeights(column, 2), hybrid.ReelWeights(column, 2), "Enhanced WILD weights must use the original high-frequency table.");
            CollectionAssert.AreNotEqual(hybrid.ReelWeights(column, 1), hybrid.ReelWeights(column, 2), "Boost must no longer select an identical table.");
        }
        CollectionAssert.AreEqual(ordinaryLoader.Value.Rrggiomg.GpinGqorrgrRonpom,
            hybridLoader.Value.Rrggiomg.GpinGqorrgrRonpom, "Scatter frequency must stay ordinary.");
        CollectionAssert.AreEqual(highLoader.Value.Rgpggm.Qogt, hybridLoader.Value.Rgpggm.Qogt,
            "High withdrawal thresholds must be retained.");
    }

    private static GoldenDragonAutoGenConfig IsolatedConfig(GoldenDragonAutoGenConfig source, int count)
    {
        var config = JsonUtility.FromJson<GoldenDragonAutoGenConfig>(JsonUtility.ToJson(source));
        var ordinary = new List<int> { 1000 };
        // A fixed Unity seed avoids the original inclusive random-zero edge case.
        // This test isolates table selection; it does not estimate production odds.
        var boosted = new List<int> { 0, 0, 0, 1000000 };
        var symbols = config.Gimrol;
        symbols.Rggl1 = symbols.Rggl2 = symbols.Rggl3 = symbols.Rggl4 = symbols.Rggl5 = ordinary;
        symbols.RgglKilp1 = symbols.RgglKilp2 = symbols.RgglKilp3 = symbols.RgglKilp4 = symbols.RgglKilp5 = ordinary;
        symbols.MorgKilp1 = symbols.MorgKilp2 = symbols.MorgKilp3 = symbols.MorgKilp4 = symbols.MorgKilp5 = boosted;
        symbols.MorgKilpRimgg = new List<int> { count };
        symbols.KilpGpinQP = new List<int> { 1000 };
        config.Ronig.MinGpin = new List<int> { 1000 };
        config.Ronig.QoinGpinKgiitr = ordinary;
        config.Rrggiomg.GpinGqorrgrRonpom = ordinary;
        return config;
    }

    private static int CountWilds(RecoveredSlotBoard board)
    {
        int count = 0;
        for (int column = 0; column < 5; column++)
            for (int row = 0; row < 3; row++)
                if (board.GetSymbol(column, row) == 7) count++;
        return count;
    }

    private static void Finish(RecoveredSpinResult result)
    {
        int steps = 0;
        while (result.Step()) Assert.Less(++steps, 100, "Isolated result failed to finish.");
    }
}
