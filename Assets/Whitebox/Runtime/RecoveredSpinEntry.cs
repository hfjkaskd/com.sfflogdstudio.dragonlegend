using System;
using DragonLegend.Whitebox.Recovered;

namespace DragonLegend.Whitebox
{
    // The paid SpinBtn branch of UIMainView.OnClickButton (0x23bd4e8).
    // View events require the recovered prefab presenter; this is not the later
    // reel animation / reward-branch completion state machine.
    public sealed class RecoveredSpinEntry
    {
        private readonly RecoveredGameplayRules rules;
        private readonly PlayerData data;
        private readonly RecoveredPlayerProgress progress;
        private readonly RecoveredSpinResult result;
        private readonly Action save;
        public event Action MoreSpinsRequested;
        public event Action StartVisualsRequested;
        public event Action GuideHideRequested;
        public event Action WinResetRequested;
        public event Action JackpotAnimationsRequested;
        public event Action<int> CountdownRequested;
        public event Action SpinCountDisplayRequested;

        public RecoveredSpinEntry(RecoveredGameplayRules rules, PlayerData data,
            RecoveredPlayerProgress progress, RecoveredSpinResult result, Action save)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.result = result ?? throw new ArgumentNullException(nameof(result));
            this.save = save ?? throw new ArgumentNullException(nameof(save));
        }

        // isSpinning belongs to the presenter and remains true through rewards.
        // now is supplied by the caller's UTC clock, identically on all platforms.
        public bool TryBegin(bool isSpinning, int bet, string configType, long now, Action<int> onReelsReady = null)
        {
            if (isSpinning || result.IsGenerating) return false;
            if (progress.SpinCount <= 0) { MoreSpinsRequested?.Invoke(); return false; }
            data.isStartSpin = true;
            StartVisualsRequested?.Invoke();
            bool isGuide = data.GuideStep == 1;
            if (isGuide) { data.GuideStep++; GuideHideRequested?.Invoke(); }
            int count = unchecked(data.LimitSpinCount + 1);
            data.LimitSpinCount = count < 0 ? 0 : Math.Min(count, rules.GetLimitMaxSpinCount());
            // Preserve saved milestones; no SDK tracking calls are introduced.
            if ((data.LimitSpinCount >= 10 && data.SpinCountLog == 0) ||
                (data.LimitSpinCount >= 20 && data.SpinCountLog == 1))
            {
                data.SpinCountLog++;
                save();
            }
            progress.SetSpinCount(unchecked(progress.SpinCount - 1));
            progress.SetExperience(progress.Experience + 1);
            progress.SetJpAddCount(unchecked(progress.JpAddCount + 1));
            progress.SetBankCount(unchecked(progress.BankCount + 1));
            WinResetRequested?.Invoke();
            JackpotAnimationsRequested?.Invoke();
            if (progress.MoreWild > 0) progress.SetMoreWild(progress.MoreWild - 1);
            if(configType!="default")SpinCountDisplayRequested?.Invoke();
            if (configType == "default" && progress.SpinCount == unchecked(rules.GetMaxSpinCount() - 1))
            {
                // Original writes directly, without an additional Save here.
                data.LastSpinTime = unchecked((int)now);
                CountdownRequested?.Invoke(rules.GetSpinCD(progress.Level));
            }
            result.Begin(isGuide, bet, progress.MoreWild, data.BonusArea, onReelsReady);
            return true;
        }
    }
}
