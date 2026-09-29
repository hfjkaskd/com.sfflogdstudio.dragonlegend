using System;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class RecoveredFreeSpinGuaranteeTests
{
    private static readonly int[] FillerIds = { 0, 1, 2, 3, 4, 5, 6 };

    private static GoldenDragonAutoGenConfig Load(string name)
        => JsonUtility.FromJson<GoldenDragonAutoGenConfig>(File.ReadAllText(
            Path.Combine(Application.streamingAssetsPath, "RecoveredConfig/Remote/" + name + ".json")));

    private static void Complete(RecoveredFreeSpinResult result)
    {
        int steps = 0;
        while (result.IsGenerating && steps++ < 10000) result.Step();
        Assert.IsFalse(result.IsGenerating, "The guaranteed board must fit and finish generating.");
    }

    private static int Count(RecoveredFreeSpinResult result, int symbol)
    {
        int count = 0;
        for (int column = 0; column < 5; column++)
            for (int row = 0; row < 3; row++)
                if (result.GetSymbol(column, row) == symbol) count++;
        return count;
    }

    [Test]
    public void MissingJsonFieldRetainsTheOriginalZeroCoinOutcome()
    {
        var config = JsonUtility.FromJson<GoldenDragonAutoGenConfig>(
            "{\"Rrggiomg\":{\"QoinOmoinrKgiitr\":[1000],\"RollOmoinrKgiitr\":[1000],\"RollRipgKgiitr\":[1000]}}");
        var rules = new RecoveredGameplayRules(config);
        Assert.AreEqual(0, config.Rrggiomg.MinimumCoinsPerSpin);
        Assert.AreEqual(0, rules.GetMinimumFreeCoinsPerSpin());
        var saved = Random.state;
        try
        {
            Random.InitState(1931);
            var result = new RecoveredFreeSpinResult(rules);
            result.Begin(FillerIds); Complete(result);
            Assert.AreEqual(0, result.CoinAmount);
            Assert.AreEqual(0, result.BallAmount);
            Assert.AreEqual(0, Count(result, 9));
            Assert.AreEqual(0, Count(result, 11));
        }
        finally { Random.state = saved; }
    }

    [Test]
    public void OriginalSnapshotsStayOptOutAndCurrentConfigurationRoundTrips()
    {
        foreach (string name in new[] { "cp_default_1", "cp_test" })
            Assert.AreEqual(0, new RecoveredGameplayRules(Load(name)).GetMinimumFreeCoinsPerSpin(), name);
        var config = Load("cp_low_frequency_high_rewards");
        Assert.AreEqual(1, config.Rrggiomg.MinimumCoinsPerSpin);
        var restored = JsonUtility.FromJson<GoldenDragonAutoGenConfig>(JsonUtility.ToJson(config));
        Assert.AreEqual(1, new RecoveredGameplayRules(restored).GetMinimumFreeCoinsPerSpin());
        CollectionAssert.AreEqual(config.Rrggiomg.QoinOmoinrKgiitr, restored.Rrggiomg.QoinOmoinrKgiitr);
        CollectionAssert.AreEqual(config.Rrggiomg.RollOmoinrKgiitr, restored.Rrggiomg.RollOmoinrKgiitr);
        CollectionAssert.AreEqual(config.Ronig.QoinRgkorp, restored.Ronig.QoinRgkorp);
    }

    [Test]
    public void GuaranteeKeepsOrdinaryEventWeightsAndFreeSpinCounts()
    {
        var ordinary = Load("cp_default_1").Rrggiomg;
        var current = Load("cp_low_frequency_high_rewards").Rrggiomg;
        CollectionAssert.AreEqual(ordinary.GpinGqorrgrRonpom, current.GpinGqorrgrRonpom);
        CollectionAssert.AreEqual(ordinary.QoinOmoinrKgiitr, current.QoinOmoinrKgiitr);
        CollectionAssert.AreEqual(ordinary.RollOmoinrKgiitr, current.RollOmoinrKgiitr);
        CollectionAssert.AreEqual(ordinary.RollRipgKgiitr, current.RollRipgKgiitr);
        CollectionAssert.AreEqual(ordinary.RrggGping, current.RrggGping);
        CollectionAssert.AreEqual(ordinary.GjrroRrggGping, current.GjrroRrggGping);
        CollectionAssert.AreEqual(ordinary.RollGlorg, current.RollGlorg);
        CollectionAssert.AreEqual(ordinary.RollKtggl, current.RollKtggl);
        CollectionAssert.AreEqual(ordinary.RollRrgogirg, current.RollRrgogirg);
        CollectionAssert.AreEqual(ordinary.RollLiqki, current.RollLiqki);
    }

    [Test]
    public void ZeroDrawGetsOneCoinWithoutReducingNaturalWinsOrRedrawingBalls()
    {
        var rules = new RecoveredGameplayRules(Load("cp_low_frequency_high_rewards"));
        var saved = Random.state;
        int zeroDraws = 0, largerDraws = 0;
        try
        {
            for (int seed = 0; seed < 512; seed++)
            {
                Random.InitState(91600 + seed);
                int naturalCoins = rules.GetFreeCoinAmount(), expectedBalls = rules.GetFreeBallAmount();
                for (int cell = 0; cell < 15; cell++) Random.Range(0, FillerIds.Length);
                int expectedNext = Random.Range(0, int.MaxValue);
                Random.InitState(91600 + seed);
                var result = new RecoveredFreeSpinResult(rules);
                result.Begin(FillerIds);
                // Compare before placement: the added coin intentionally consumes later
                // placement draws, but must not change this spin's original count draws.
                var afterBegin = Random.state;
                Assert.AreEqual(expectedNext, Random.Range(0, int.MaxValue), "Count/filler draw order changed.");
                Random.state = afterBegin;
                Complete(result);
                Assert.AreEqual(naturalCoins == 0 ? 1 : naturalCoins, result.CoinAmount);
                Assert.AreEqual(result.CoinAmount, Count(result, 9));
                Assert.AreEqual(expectedBalls, result.BallAmount);
                Assert.AreEqual(expectedBalls, Count(result, 11));
                Assert.AreEqual(expectedBalls, result.GeneratedBallCount);
                if (naturalCoins == 0) zeroDraws++;
                if (naturalCoins > 1) largerDraws++;
            }
            Assert.Greater(zeroDraws, 0, "The sample must exercise the guarantee.");
            Assert.Greater(largerDraws, 0, "The sample must exercise unchanged natural multi-coin wins.");
        }
        finally { Random.state = saved; }
    }

    [TestCase(4)]
    [TestCase(6)]
    [TestCase(8)]
    [TestCase(10)]
    public void ConsecutiveFreeRoundsEachProducePositiveCoinAwards(int rounds)
    {
        var config = Load("cp_low_frequency_high_rewards");
        var rules = new RecoveredGameplayRules(config);
        var saved = Random.state;
        try
        {
            Random.InitState(1931 + rounds);
            var result = new RecoveredFreeSpinResult(rules);
            int total = 0;
            for (int round = 0; round < rounds; round++)
            {
                result.Begin(FillerIds); Complete(result);
                int reward = 0, coins = Count(result, 9);
                Assert.GreaterOrEqual(coins, 1, "Free round " + round);
                for (int coin = 0; coin < coins; coin++)
                {
                    int amount = rules.GetCoinReward();
                    Assert.That(amount, Is.InRange(config.Ronig.QoinRgkorp[0], config.Ronig.QoinRgkorp[1]));
                    reward += amount;
                }
                Assert.Greater(reward, 0, "The configured coin award must be positive every round.");
                total += reward;
            }
            Assert.GreaterOrEqual(total, rounds * config.Ronig.QoinRgkorp[0]);
            // This is generation/award verification; the separate UI integration test
            // verifies crediting and the end window, without claiming that here.
        }
        finally { Random.state = saved; }
    }
}
