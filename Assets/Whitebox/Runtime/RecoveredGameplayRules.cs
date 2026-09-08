using System;
using System.Collections.Generic;
using DragonLegend.Whitebox.Recovered;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Ported from native ConfigManager bodies, not metadata stubs.
    // Offsets refer to the original ARM64 ELF. Configuration retains original keys.
    public sealed class RecoveredGameplayRules
    {
        private readonly GoldenDragonAutoGenConfig data;
        private readonly List<int> winningCoinWeights = new List<int>();
        private List<RecoveredTaskInfo> taskInfos;
        public RecoveredGameplayRules(GoldenDragonAutoGenConfig configuration)
        {
            data = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public int GetWildSpinCD() => data.Gimrol.KilpGpinQP[0]; // 0x236b4c8
        public int GetMinSpin() => data.Ronig.MinGpin[0]; // 0x236b8d8
        public int GetCoinSpinAmount() => RandomListWeight(data.Ronig.QoinGpinKgiitr);
        public int GetCoinSpinAmountWin() // 0x236b828; removes first weight, no index +1
        {
            if (winningCoinWeights.Count == 0)
            {
                winningCoinWeights.AddRange(data.Ronig.QoinGpinKgiitr);
                winningCoinWeights.RemoveAt(0);
            }
            return RandomListWeight(winningCoinWeights);
        }
        public int GetMaxSpinCount() => data.Qonrii.MojGping[0]; // 0x236a638
        public int GetLimitMaxSpinCount() => data.Qonrii.MojGping[1]; // 0x236a69c
        public int GetBankSpinCD() => data.Qonrii.RonkGpinQP[0]; // 0x236af8c
        public int GetSpinCD(int level) // 0x236a764
        {
            var config = data.Qonrii;
            int index = level >= config.Lgtgl[config.Lgtgl.Count - 1] ? config.GpinQP.Count - 1 : level - 1;
            return config.GpinQP[index];
        }
        public int GetReview() => data.Qonrii.Rgtigk[0]; // 0x236aec4
        public int GetNeedPro(int level) // 0x236adb8
        {
            var config = data.Qonrii;
            int index = level >= config.Lgtgl[config.Lgtgl.Count - 1] ? config.NggpGpin.Count - 1 : level - 1;
            return config.NggpGpin[index];
        }
        public int GetCollectInfoCount() => data.Qollgqr.Ip.Count; // 0x236c644
        public int GetInitGreenCount() => data.Qonrii.InirQoing[0]; // 0x236a570
        public int GetInitSpinCount() => data.Qonrii.InirGping[0]; // 0x236a5d4
        public int GetLines() => data.Gimrol.Lingg[0]; // 0x236b73c
        public string GetConfigType() => data.Qonrii.Ripg[0]; // 0x236a248
        public int GetCashOutCount() => data.Rgpggm.Qogt.Count; // 0x236ccc8
        public float GetCashOutCash(int index) => data.Rgpggm.Qogt[index]; // 0x236cd20; no currency conversion

        // 0x236a870. Destination is caller-owned to avoid allocating a list per refresh.
        public void GetBet(bool isA, int level, List<int> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (ReferenceEquals(destination, data.Qonrii.Rgr) || ReferenceEquals(destination, data.Qonrii.Rgrlgtgl))
                throw new ArgumentException("Output must not alias configuration.", nameof(destination));
            destination.Clear();
            if (!isA) { destination.Add(data.Qonrii.Rgr[0]); return; }
            var thresholds = data.Qonrii.Rgrlgtgl;
            if (level >= thresholds[thresholds.Count - 1]) { destination.AddRange(data.Qonrii.Rgr); return; }
            for (int i = 0; i < thresholds.Count; i++)
            {
                if (level < thresholds[i]) return;
                destination.Add(data.Qonrii.Rgr[i]);
            }
        }

        // 0x236b688. Original dispatch uses J5 for every count other than 3 and 4.
        public int GetPay(int symbol, int count)
        {
            return (count == 3 ? data.Gimrol.J3 : count == 4 ? data.Gimrol.J4 : data.Gimrol.J5)[symbol];
        }

        public int RandomReelSymbol(int reel) => RandomListWeight(ReelWeights(reel, 0)); // 0x236b42c
        public int RandomWildSymbol(int reel) => RandomListWeight(ReelWeights(reel, 1)); // 0x236b550
        public int RandomMoreWildSymbol(int reel) => RandomListWeight(ReelWeights(reel, 2)); // 0x236b5ec
        public int RandomWildCount() => RandomListWeight(data.Gimrol.KilpGpinKgiitr); // 0x236b52c; returns INDEX
        public int GetSpinScatterAmount() => RandomListWeight(data.Rrggiomg.GpinGqorrgrRonpom); // 0x236bea8; returns INDEX
        public int GetFreeCoinAmount() => RandomListWeight(data.Rrggiomg.QoinOmoinrKgiitr); // 0x236bf98
        public int GetFreeBallAmount() => RandomListWeight(data.Rrggiomg.RollOmoinrKgiitr); // 0x236bfbc
        public int GetFreeBallType() // 0x236bfe0: all indices other than 0 and 1 map to 2
        {
            int index = RandomListWeight(data.Rrggiomg.RollRipgKgiitr);
            return index == 0 ? 0 : index == 1 ? 1 : 2;
        }
        public int GetFreeSpins(int scatterCount) => data.Rrggiomg.RrggGping[scatterCount]; // 0x236becc

        // 0x236c9f0: constructs once, in configured ID order.
        public IReadOnlyList<RecoveredTaskInfo> GetTaskInfos()
        {
            if (taskInfos != null) return taskInfos;
            taskInfos = new List<RecoveredTaskInfo>();
            var config = data.Rogk;
            for (int i = 0; i < config.Ip.Count; i++)
                taskInfos.Add(new RecoveredTaskInfo {id=config.Ip[i],taskAmount=config.RogkOmoinr[i],
                    reward=config.Rgkorp[i],jump=config.Jimp[i]});
            return taskInfos;
        }

        public IReadOnlyList<int> ReelWeights(int reel, int mode)
        {
            var s = data.Gimrol;
            switch (mode)
            {
                case 0:
                    switch (reel) { case 0: return s.Rggl1; case 1: return s.Rggl2; case 2: return s.Rggl3; case 3: return s.Rggl4; default: return s.Rggl5; }
                case 1:
                    switch (reel) { case 0: return s.RgglKilp1; case 1: return s.RgglKilp2; case 2: return s.RgglKilp3; case 3: return s.RgglKilp4; default: return s.RgglKilp5; }
                case 2:
                    switch (reel) { case 0: return s.MorgKilp1; case 1: return s.MorgKilp2; case 2: return s.MorgKilp3; case 3: return s.MorgKilp4; default: return s.MorgKilp5; }
                default: throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        // 0x236a380. Preserve integer sum and inclusive floating point boundary.
        public static int RandomListWeight(IReadOnlyList<int> weights)
        {
            int total = 0;
            for (int i = 0; i < weights.Count; i++) total = unchecked(total + weights[i]);
            return RecoveredConfigCodec.SelectAt(weights, UnityEngine.Random.Range(0, total));
        }

        // 0x236cd90, 0x236ced0, 0x236d010. Out-of-range tier returns to tier zero
        // in the original search loop; this is not a clamp-to-last operation.
        private int TaskTier(int index) => index >= 0 && index < GetCashOutCount() ? index : 0;
        public int GetSuccessTaskCount(int index, int step)
        {
            var c = data.Rgpggm;
            List<int> values;
            switch (step) { case 0: values = c.Rogk1tir; break; case 1: values = c.Rogk2tir; break; case 2: values = c.Rogk3tir; break; case 3: values = c.Rogk4tir; break; case 4: values = c.Rogk5tir; break; default: values = c.Rogk6tir; break; }
            return values[TaskTier(index)];
        }
        public int GetFailTaskCount(int index, int step)
        {
            var c = data.Rgpggm;
            List<int> values;
            switch (step) { case 0: values = c.Rogk1roil; break; case 1: values = c.Rogk2roil; break; case 2: values = c.Rogk3roil; break; case 3: values = c.Rogk4roil; break; case 4: values = c.Rogk5roil; break; default: values = c.Rogk6roil; break; }
            return values[TaskTier(index)];
        }
        public int GetWaitTime(int index, int step)
        {
            var c = data.Rgpggm;
            List<int> values;
            switch (step) { case 0: values = c.Rimgg1; break; case 1: values = c.Rimg2; break; case 2: values = c.Rimg3; break; case 3: values = c.Rimg4; break; case 4: values = c.Rimg5; break; case 5: values = c.Rimg6; break; default: values = c.Rimg7; break; }
            return values[TaskTier(index)];
        }
    }
}
