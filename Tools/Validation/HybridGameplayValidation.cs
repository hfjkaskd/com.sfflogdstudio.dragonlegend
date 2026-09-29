using System;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using UnityEngine;

// Temporary Editor-only executeMethod harness. No SDK, network or persisted player data.
public static class HybridGameplayValidation
{
    private static readonly List<string> checks = new List<string>();
    [Serializable] private sealed class Report { public string status; public string[] checks; }
    public static void Run()
    {
        var randomState = UnityEngine.Random.state;
        checks.Clear();
        try
        {
            var hybrid = Read("cp_low_frequency_high_rewards.json");
            var low = Read("cp_default_1.json");
            var high = Read("cp_test.json");
            var rules = new RecoveredGameplayRules(hybrid);
            var highRules = new RecoveredGameplayRules(high);
            CheckProbabilities(hybrid, low, high);
            CheckRewards(rules, highRules);
            CheckCashOut(rules, highRules);
            CheckSpinContinuation(rules);
            var report = new Report { status = "PASS", checks = checks.ToArray() };
            string json = JsonUtility.ToJson(report, true);
            string path = Path.Combine(Application.dataPath, "../Temp/HybridGameplayValidation.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
            Debug.Log("HYBRID_GAMEPLAY_VALIDATION_PASS\n" + json);
        }
        finally { UnityEngine.Random.state = randomState; }
    }
    private static GoldenDragonAutoGenConfig Read(string file)
    {
        string path = Path.Combine(Application.streamingAssetsPath, "RecoveredConfig/Remote", file);
        var result = JsonUtility.FromJson<GoldenDragonAutoGenConfig>(File.ReadAllText(path));
        Require(result != null && result.Qonrii != null && result.Rgpggm != null, "JSON " + file);
        return result;
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("HYBRID_VALIDATION_FAILED: " + message);
    }
    private static void Same(IReadOnlyList<int> actual, IReadOnlyList<int> expected, string name)
    {
        Require(actual.Count == expected.Count, name + " length");
        for (int i = 0; i < actual.Count; i++) Require(actual[i] == expected[i], name + " index " + i);
    }
    private static int CountSamples(IReadOnlyList<int> weights, int minimum, out int total)
    {
        total = 0;
        for (int i = 0; i < weights.Count; i++) total += weights[i];
        int hits = 0;
        for (int draw = 0; draw < total; draw++)
            if (RecoveredConfigCodec.SelectAt(weights, draw) >= minimum) hits++;
        return hits;
    }
    private static void CheckProbabilities(GoldenDragonAutoGenConfig hybrid, GoldenDragonAutoGenConfig low, GoldenDragonAutoGenConfig high)
    {
        Same(hybrid.Rrggiomg.GpinGqorrgrRonpom, low.Rrggiomg.GpinGqorrgrRonpom, "Scatter weights");
        Same(hybrid.Rrggiomg.RollOmoinrKgiitr, low.Rrggiomg.RollOmoinrKgiitr, "Free ball weights");
        var a = new RecoveredGameplayRules(hybrid); var b = new RecoveredGameplayRules(low);
        for (int mode = 0; mode < 2; mode++)
            for (int reel = 0; reel < 5; reel++) Same(a.ReelWeights(reel, mode), b.ReelWeights(reel, mode), "Reel weights");
        var boost = new RecoveredGameplayRules(high);
        for (int reel = 0; reel < 5; reel++)
            Same(a.ReelWeights(reel, 2), boost.ReelWeights(reel, 2), "Claimed More WILD weights");
        Require(a.GetWildSpinCD() == b.GetWildSpinCD(), "Low Wild guarantee period");
        Require(a.GetMinSpin() == b.GetMinSpin(), "Low Bonus coin period");
        Require(a.GetMoreWild() == b.GetMoreWild(), "Low MoreWild duration");
        int scatter = CountSamples(hybrid.Rrggiomg.GpinGqorrgrRonpom, 3, out int scatterTotal);
        int balls = CountSamples(hybrid.Rrggiomg.RollOmoinrKgiitr, 1, out int ballTotal);
        Require(scatter * 1000 == scatterTotal * 29, "Exact eligible Scatter probability 2.9%");
        Require(balls * 1000 == ballTotal * 499, "Exact free-turn ball probability 49.9%");
        checks.Add("SelectAt enumeration: Scatter " + scatter + "/" + scatterTotal + "; free ball " + balls + "/" + ballTotal);
        checks.Add("10 ordinary reel/WILD arrays and Wild/Bonus periods/MoreWild duration match ordinary; 5 claimed More WILD arrays match high");
    }
    private static void CheckRewards(RecoveredGameplayRules rules, RecoveredGameplayRules high)
    {
        var bets = new List<int>(); rules.GetBet(false, 1, bets);
        Require(bets.Count == 1 && bets[0] == 1000, "Main mode bet is 1000 raw / $10");
        Require(rules.GetLines() == 180 && high.GetLines() == 180, "High line divisor 180");
        var a = new RecoveredSlotSettlement(rules); var b = new RecoveredSlotSettlement(high);
        int cases = 0; float example = 0;
        for (int symbol = 0; symbol < 8; symbol++)
            for (int count = 3; count <= 5; count++)
            {
                var board = new int[5, 3];
                for (int col = 0; col < 5; col++) for (int row = 0; row < 3; row++) board[col, row] = 8;
                for (int col = 0; col < 5; col++) board[col, 0] = col < count ? symbol : (symbol + 1) % 7;
                a.Evaluate(board, 1000); b.Evaluate(board, 1000);
                Require(a.RawAward == b.RawAward, "Same-board high reward: symbol/count " + symbol + "/" + count);
                Require(a.RawAward > 0, "Positive winning board");
                if (symbol == 0 && count == 3) example = a.RawAward;
                Require(a.GetWinTotalLine() == b.GetWinTotalLine(), "Same collected line reward");
                cases++;
            }
        Require(rules.GetBigWin(199, 1000) == RecoveredSlotWinType.None, "Below $2 no BigWin");
        Require(rules.GetBigWin(200, 1000) == RecoveredSlotWinType.Big, "$2 BigWin");
        Require(rules.GetBigWin(499, 1000) == RecoveredSlotWinType.Big, "Below $5 still BigWin");
        Require(rules.GetBigWin(500, 1000) == RecoveredSlotWinType.Mega, "$5 MegaWin");
        Require(rules.GetBigWin(999, 1000) == RecoveredSlotWinType.Mega, "Below $10 still MegaWin");
        Require(rules.GetBigWin(1000, 1000) == RecoveredSlotWinType.Super, "$10 SuperWin");
        Require(RecoveredCurrency.Format(2000, 0) == "$20.00", "Currency display conversion");
        checks.Add(cases + " fixed winning boards match high settlement; first raw award=" + example + "; divisor=180");
        checks.Add("Big/Mega/Super percentage thresholds and boundaries verified: $2/$5/$10 at $10 bet");
    }
    private static void CheckCashOut(RecoveredGameplayRules rules, RecoveredGameplayRules high)
    {
        int[] expected = { 50000, 100000, 300000, 500000, 1000000 };
        Require(rules.GetCashOutCount() == expected.Length, "Five cashout tiers");
        var data = new PlayerData { GreenCount = 1000000 };
        var player = new RecoveredPlayerProgress(rules, () => { }, data);
        for (int tier = 0; tier < expected.Length; tier++)
        {
            Require(rules.GetCashOutCash(tier) == expected[tier], "Cashout amount " + tier);
            Require(player.GetCashOutConditions(tier, 1000).ShowActionRow, "Cashout tier accessible " + tier);
            player.BeginCashOutReview(tier, 1, 1000);
            Require(player.CashOutRecords.Count == tier + 1, "Review record created " + tier);
            var record = player.CashOutRecords[tier];
            for (int step = 0; step <= 6; step++)
            {
                Require(rules.GetSuccessTaskCount(tier, step) == high.GetSuccessTaskCount(tier, step), "Success task array");
                Require(rules.GetFailTaskCount(tier, step) == high.GetFailTaskCount(tier, step), "Fail task array");
                Require(rules.GetWaitTime(tier, step) == high.GetWaitTime(tier, step), "Wait time array");
                record.step = step;
                var conditions = player.GetCashOutConditions(tier, 1000);
                Require(conditions.HasRecord && !conditions.RequiresOrderStatus, "Actual task conditions");
            }
        }
        checks.Add("Five cashout tiers $500/$1000/$3000/$5000/$10000: in-memory reviews created and all 7 task stages accessed");
    }
    private static void CheckSpinContinuation(RecoveredGameplayRules rules)
    {
        Require(rules.GetConfigType() == "default", "Default continuation behavior");
        Require(rules.GetInitSpinCount() == 20 && rules.GetMaxSpinCount() == 30 && rules.GetLimitMaxSpinCount() == 999, "Spin limits 20/30/999");
        var data = new PlayerData { SpinCount = rules.GetInitSpinCount(), GuideStep = 2 };
        var player = new RecoveredPlayerProgress(rules, () => { }, data);
        var result = new RecoveredSpinResult(rules, new RecoveredSlotSettlement(rules), player);
        var entry = new RecoveredSpinEntry(rules, data, player, result, () => { });
        var ads = new FakeAds(); var view = new FakeView();
        var claim = new RecoveredMoreSpinClaim(rules, player, data, ads, view);
        UnityEngine.Random.InitState(15092026);
        for (int spin = 1; spin <= 65; spin++)
        {
            if (player.SpinCount == 0) { claim.BeforeShow(); claim.Click("ClaimBtn"); }
            Require(entry.TryBegin(false, 1000, rules.GetConfigType(), 1000 + spin), "Can begin base spin " + spin);
            int steps = 0;
            while (result.IsGenerating && steps++ < 4096) result.Step();
            Require(!result.IsGenerating, "Bounded result generation " + spin);
        }
        Require(data.LimitSpinCount == 65 && ads.RewardCount > 0 && view.LimitTips == 0, "Continues beyond 60 spins");
        data.LimitSpinCount = 999; player.SetSpinCount(0);
        int before = ads.RewardCount; claim.BeforeShow(); claim.Click("ClaimBtn");
        Require(ads.RewardCount == before + 1 && player.SpinCount == rules.GetAddSpins(), "Default can replenish even at 999");
        checks.Add("65 actual base SpinEntry/result generations completed with in-memory ad replenishment; 60 and 999 gates pass");
    }
    private sealed class FakeAds : IAdFacade
    {
        public int RewardCount;
        public void PlayRewardAd(Action success, Action failure, string placement, string scene)
        { Require(placement == "extraspin", "Only local extraspin probe"); RewardCount++; success(); }
        public void PlayInterAd(string placement, string scene) { throw new InvalidOperationException("Unexpected interstitial"); }
    }
    private sealed class FakeView : IRecoveredMoreSpinView
    {
        public int LimitTips;
        public void PlaySound(string sound) { }
        public void ShowLimitTip() { LimitTips++; }
        public void Hide() { }
    }
}
