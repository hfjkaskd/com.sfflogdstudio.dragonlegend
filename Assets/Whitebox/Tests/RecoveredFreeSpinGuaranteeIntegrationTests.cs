using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredFreeSpinGuaranteeIntegrationTests
{
    [UnityTest]
    public IEnumerator SixOriginallyEmptyRoundsPayGuaranteedCoinsAndDisplaySixDollars()
    {
        const int roundCount = 6;
        const int coinReward = 100;
        var random = Random.state;
        float timeScale = Time.timeScale, captureDelta = Time.captureDeltaTime;
        GameObject root = null;
        RecoveredFreeReels reels = null;
        RecoveredFreeExitFlow exit = null;
        try
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = .05f;
            Random.InitState(160926);
            var rules = new RecoveredGameplayRules(new GoldenDragonAutoGenConfig
            {
                Ronig = new RonigPoro { QoinRgkorp = new List<int> { coinReward, coinReward } },
                Rrggiomg = new RrggiomgPoro
                {
                    // Both original weighted results are always zero. Only the configured
                    // minimum can introduce a paying symbol into these real stopped boards.
                    QoinOmoinrKgiitr = new List<int> { 1000000 },
                    RollOmoinrKgiitr = new List<int> { 1000000 },
                    RollRipgKgiitr = new List<int> { 1000000 },
                    MinimumCoinsPerSpin = 1
                }
            });
            Assert.AreEqual(0, rules.GetFreeCoinAmount());
            Assert.AreEqual(0, rules.GetFreeBallAmount());
            Assert.AreEqual(1, rules.GetMinimumFreeCoinsPerSpin());

            int saves = 0, started = 0, settled = 0;
            const float startingBalance = 250;
            float lastBalance = startingBalance;
            var data = new PlayerData
            {
                GreenCount = startingBalance,
                BonusArea = new List<int> { 0, 0, 0, 0, 0 }
            };
            // The save callback is in-memory only; this fixture never opens a player store.
            var player = new RecoveredPlayerProgress(rules, () => saves++, data)
            {
                GameSlotType = RecoveredSlotType.Free,
                FreeSpinCount = roundCount
            };
            var symbols = new[] { 3 };
            var result = new RecoveredFreeSpinResult(rules);
            result.Begin(symbols);
            for (int step = 0; result.IsGenerating && step < 100; step++) result.Step();
            Assert.IsFalse(result.IsGenerating);

            // Only a test host Canvas is created in code. All production visuals and
            // reward/exit components below are instantiated from their actual prefabs.
            root = new GameObject("Free spin guarantee integration", typeof(RectTransform), typeof(Canvas));
            var catalog = Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");
            var reelsPrefab = Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels");
            var collectionPrefab = Resources.Load<RecoveredBonusCollection>("RecoveredUI/BonusCollection");
            var textPrefab = Resources.Load<RecoveredDownWinText>("RecoveredUI/DownWinText");
            var exitPrefab = Resources.Load<RecoveredFreeExitFlow>("RecoveredUI/FreeExitFlow");
            Assert.IsNotNull(catalog);
            Assert.IsNotNull(reelsPrefab);
            Assert.IsNotNull(collectionPrefab);
            Assert.IsNotNull(textPrefab);
            Assert.IsNotNull(exitPrefab);
            reels = Object.Instantiate(reelsPrefab, root.transform, false);
            reels.Initialize(catalog, result);
            var collection = Object.Instantiate(collectionPrefab, root.transform, false);
            collection.Initialize(data.BonusArea);
            var downWin = Object.Instantiate(textPrefab, root.transform, false);
            downWin.Bind(0);
            reels.CoinScan.Bind(rules, player, result, collection, () => 0);
            reels.RewardCollect.Bind(result, player, downWin, (RectTransform)root.transform, 300);
            // Ball count is fixed at zero: this is the same continuation without
            // constructing an unrelated small-game router or SDK-backed GameEntry.
            reels.CoinScan.Completed += reels.RewardCollect.Begin;

            reels.Controller.RoundStarted += () =>
            {
                started++;
                Assert.AreEqual(settled + 1, started, "The next spin waits for the preceding settlement.");
                Assert.AreEqual(roundCount - started, player.FreeSpinCount);
                Assert.AreEqual(1, result.CoinAmount);
                Assert.AreEqual(0, result.BallAmount);
                Assert.AreEqual(0, reels.CoinScan.Rewards.Count, "Only the per-round ledger is cleared.");
                Assert.AreEqual(settled * coinReward, player.TotalFreeSpinWin);
                int payingCells = 0;
                for (int column = 0; column < 5; column++)
                    for (int row = 0; row < 3; row++)
                        if (result.GetSymbol(column, row) == 9) payingCells++;
                Assert.AreEqual(1, payingCells, "The minimum must produce a real result symbol.");
            };
            // Register before ExitFlow so each settled ledger is checked before its
            // automatic next-spin callback clears the per-round dictionary.
            reels.RewardCollect.Completed += () =>
            {
                settled++;
                Assert.AreEqual(started, settled);
                Assert.AreEqual(1, reels.CoinScan.Rewards.Count);
                foreach (var value in reels.CoinScan.Rewards.Values) Assert.AreEqual(coinReward, value);
                Assert.AreEqual(settled * coinReward, player.TotalFreeSpinWin,
                    "Each guaranteed coin is counted once; collection must not add it again.");
                Assert.AreEqual(settled * coinReward, reels.RewardCollect.FreeReward);
                Assert.AreEqual(settled * coinReward, reels.RewardCollect.CoinReward);
                Assert.Greater(player.GreenCount, lastBalance, "Every completed round actually credits the balance.");
                lastBalance = player.GreenCount;
                Assert.AreEqual(settled * 2, saves);
                // Native CoinReward is cumulative and is credited each round. This
                // guarantee test deliberately does not alter that existing behavior.
            };

            var spinEntry = new RecoveredFreeSpinEntry(player, result);
            exit = Object.Instantiate(exitPrefab, root.transform, false);
            exit.Bind(reels, player, result, spinEntry, symbols, () => roundCount, root.transform, () => 0);
            Assert.IsTrue(spinEntry.TryBegin(symbols));
            for (int step = 0; result.IsGenerating && step < 100; step++) result.Step();
            Assert.IsFalse(result.IsGenerating);

            for (int frame = 0; frame < 2500 && !exit.Window.IsShown; frame++)
            {
                Assert.IsNull(reels.Controller.Error);
                Assert.IsNull(reels.CoinScan.Error);
                Assert.IsNull(reels.RewardCollect.Error);
                Assert.IsNull(exit.Error);
                yield return null;
            }
            Assert.IsNull(reels.Controller.Error);
            Assert.IsNull(reels.CoinScan.Error);
            Assert.IsNull(reels.RewardCollect.Error);
            Assert.IsNull(exit.Error);
            Assert.AreEqual(roundCount, started);
            Assert.AreEqual(roundCount, settled);
            Assert.AreEqual(0, player.FreeSpinCount);
            Assert.AreEqual(RecoveredSlotType.Base, player.GameSlotType);
            Assert.AreEqual(600, player.TotalFreeSpinWin);
            Assert.Greater(player.GreenCount, startingBalance);
            Assert.IsTrue(exit.Window.IsShown);

            float balanceBeforeSummary = player.GreenCount;
            int savesBeforeSummary = saves;
            for (int frame = 0; frame < 200 && !exit.Window.ContinueButton.gameObject.activeSelf; frame++) yield return null;
            Assert.IsNull(exit.Error);
            Assert.IsTrue(exit.Window.ContinueButton.gameObject.activeSelf);
            Assert.AreEqual("$6.00", exit.Window.TotalText.text,
                "The real summary must display the six recorded guaranteed rewards.");
            Assert.AreEqual(600, player.TotalFreeSpinWin);
            Assert.AreEqual(balanceBeforeSummary, player.GreenCount, "The summary does not pay the rewards twice.");
            Assert.AreEqual(savesBeforeSummary, saves);
        }
        finally
        {
            if (exit != null) exit.Unbind();
            if (reels != null) reels.CoinScan.Completed -= reels.RewardCollect.Begin;
            if (root != null)
            {
                root.SetActive(false);
                Object.Destroy(root);
            }
            Random.state = random;
            Time.timeScale = timeScale;
            Time.captureDeltaTime = captureDelta;
        }
    }
}
