using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredFreeRewardCollectTests
{
    [UnityTest]
    public IEnumerator ActualMixedStopsReadLiveLedgerAndCreditOnlyCoinsAfterAllFlights()
    {
        var random = Random.state; float scale = Time.timeScale, delta = Time.captureDeltaTime;
        var coins = new List<int>(); for (int i = 0; i <= 13; i++) coins.Add(i == 13 ? 1000000 : 0);
        var rules = new RecoveredGameplayRules(new GoldenDragonAutoGenConfig { Rrggiomg = new RrggiomgPoro {
            QoinOmoinrKgiitr = coins, RollOmoinrKgiitr = new List<int> { 0, 0, 1000000 }, RollRipgKgiitr = new List<int> { 1000000 }
        }});
        var result = new RecoveredFreeSpinResult(rules); result.Begin(new[] { 3 }); while (result.IsGenerating) result.Step();
        var root = Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        root.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"), result);
        var collection = Object.Instantiate(Resources.Load<RecoveredBonusCollection>("RecoveredUI/BonusCollection"));
        var text = Object.Instantiate(Resources.Load<RecoveredDownWinText>("RecoveredUI/DownWinText")); text.Bind(0);
        var bottom = new GameObject("Collection bottom", typeof(RectTransform));
        int saves = 0, done = 0, arrivals = 0; float expected = 0, expectedCoins = 0;
        var player = new RecoveredPlayerProgress(rules, () => saves++, new PlayerData { GreenCount = 20 });
        var collect = root.RewardCollect;
        try
        {
            Time.timeScale = 1; Time.captureDeltaTime = .05f;
            root.CoinScan.Bind(rules, player, result, collection, () => 0);
            for (int i = 0; i < 15; i++)
            {
                var reel = root.At(i / 3, i % 3); root.Specials.ApplyStoppedResult(reel, i / 3, i % 3);
                int id = result.GetSymbol(i / 3, i % 3); Assert.IsTrue(id == 9 || id == 11);
                float reward = i == 0 ? 5 : i == 1 ? 0 : i == 2 ? -3 : i == 3 ? 0 : i + 1;
                if (i != 0 && i != 3) root.CoinScan.RecordReward(reel, reward);
                if (reward > 0) { expected += reward; if (id == 9) expectedCoins += reward; }
            }
            for (int column = 0; column < 5; column++) root.ColumnAt(column).ShowFreeEffects();
            collect.Bind(result, player, text, (RectTransform)bottom.transform, 300);
            collect.Completed += () => done++;
            collect.Flights.ArrivalEffectRequested += () => { Assert.AreEqual(0, collect.Flights.ActiveCount); arrivals++; };
            collect.Begin();
            Assert.AreEqual(1, collect.Flights.ActiveCount);
            Assert.AreEqual("FreeResult", collect.Flights.FlightAt(0).transform.parent.name);
            Assert.AreEqual(root.At(0, 0).transform.position, collect.Flights.FlightAt(0).transform.position);
            // Populate the first cell AFTER Begin: a captured reward would incorrectly omit it.
            root.CoinScan.RecordReward(root.At(0, 0), 5);
            float recordedTotal = player.TotalFreeSpinWin;
            Time.timeScale = 0;
            for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(0, collect.FreeReward); Assert.AreEqual(0, arrivals); Assert.AreEqual(0, saves);
            Time.timeScale = 1;
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 1) collect.Begin();
                for (int i = 0; i < 250 && done <= pass; i++)
                {
                    if (arrivals < (pass + 1) * 15) Assert.AreEqual(pass * 2, saves, "Credit follows the complete scan.");
                    yield return null;
                }
                Assert.IsNull(collect.Error); Assert.AreEqual(pass + 1, done);
                Assert.AreEqual((pass + 1) * 15, arrivals);
                Assert.AreEqual((pass + 1) * expected, collect.FreeReward);
                Assert.AreEqual((pass + 1) * expectedCoins, collect.CoinReward);
                Assert.AreEqual((pass + 1) * 2, saves);
                Assert.AreEqual(20 + (pass == 0 ? 1 : 3) * expectedCoins, player.GreenCount);
                Assert.AreEqual(recordedTotal, player.TotalFreeSpinWin, "Collection never adds the ledger to TotalFreeSpinWin twice.");
                for (int i = 0; i < 5; i++) yield return null;
                Assert.AreEqual(RecoveredCurrency.Format(collect.FreeReward, 0, 2), text.Label.text);
                for (int i = 0; i < 15; i++)
                {
                    var reel = root.At(i / 3, i % 3); int id = result.GetSymbol(i / 3, i % 3);
                    Component symbol = id == 9 ? (Component)root.Specials.CurrentStoppedCoin(reel) : root.Specials.CurrentStoppedBall(reel);
                    Assert.That(symbol.transform.localScale.x, Is.EqualTo(id == 9 ? .7f : .8f).Within(.0001f));
                }
            }
            Assert.AreEqual(1, collect.Flights.CreatedCount, "Sequential flights reuse the pool object.");
            collect.ResetSession(); Assert.AreEqual(0, collect.FreeReward); Assert.AreEqual(0, collect.CoinReward);
        }
        finally
        {
            Object.Destroy(root.gameObject); Object.Destroy(collection.gameObject); Object.Destroy(text.gameObject); Object.Destroy(bottom);
            Random.state = random; Time.timeScale = scale; Time.captureDeltaTime = delta;
        }
    }
}
