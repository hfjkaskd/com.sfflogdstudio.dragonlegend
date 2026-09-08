using System;
using DragonLegend.Whitebox.Recovered;

namespace DragonLegend.Whitebox
{
    // GameData property behavior, backed by the complete original player record.
    public sealed class RecoveredPlayerProgress
    {
        private readonly RecoveredGameplayRules rules;
        private readonly Action save;
        private readonly PlayerData data;
        public int Level { get => data.Level; private set => data.Level = value; }
        public float Experience { get => data.LevelExpCount; private set => data.LevelExpCount = value; }
        public int SpinCount { get => data.SpinCount; private set => data.SpinCount = value; }
        public int MoreWild { get => data.MoreWild; private set => data.MoreWild = value; }
        public int JpAddCount => data.JpAddCount;
        public int BankCount => data.BankCount;
        public event Action BankReady;
        public event Action BankProgressChanged;
        public event Action<int> SpinCountChanged;
        public event Action<int,float,float> LevelExperienceChanged;
        public event Action ReviewRequested;
        public event Action MoreWildChanged;

        public RecoveredPlayerProgress(RecoveredGameplayRules gameplayRules, Action savePlayer,
            int level, float experience, int spinCount, int moreWild)
            : this(gameplayRules, savePlayer, new PlayerData {Level=level,LevelExpCount=experience,SpinCount=spinCount,MoreWild=moreWild}) { }

        public RecoveredPlayerProgress(RecoveredGameplayRules gameplayRules, Action savePlayer, PlayerData playerData)
        {
            rules = gameplayRules ?? throw new ArgumentNullException(nameof(gameplayRules));
            save = savePlayer ?? throw new ArgumentNullException(nameof(savePlayer));
            data = playerData ?? throw new ArgumentNullException(nameof(playerData));
        }

        // 0x236e3c8: persist clamped value, then notify using the UNCLAMPED request.
        public void SetSpinCount(int value)
        {
            SpinCount = value < 0 ? 0 : Math.Min(value,rules.GetMaxSpinCount());
            save();
            SpinCountChanged?.Invoke(value);
        }

        // 0x236de70: one level at most, discards overflow experience. On level-up
        // the progress notification carries threshold/threshold while storage is zero.
        public void SetExperience(float value)
        {
            float threshold = rules.GetNeedPro(Level);
            if (value >= threshold)
            {
                Level = unchecked(Level + 1);
                if (Level == rules.GetReview()) ReviewRequested?.Invoke();
                Experience = 0;
                LevelExperienceChanged?.Invoke(Level,threshold,threshold);
            }
            else
            {
                Experience = value;
                LevelExperienceChanged?.Invoke(Level,Experience,threshold);
            }
            save();
        }

        // 0x236e9a4: no clamp, event before persistence.
        public void SetMoreWild(int value)
        {
            MoreWild = value;
            MoreWildChanged?.Invoke();
            save();
        }

        // 0x236e5a8: no clamp or event.
        public void SetJpAddCount(int value)
        {
            data.JpAddCount = value;
            save();
        }

        // 0x236e6b0: original event "4" at/above threshold, "5" below.
        // Notification precedes saving and repeats on every assignment.
        public void SetBankCount(int value)
        {
            data.BankCount = value;
            if (value >= rules.GetBankSpinCD()) BankReady?.Invoke();
            else BankProgressChanged?.Invoke();
            save();
        }
    }
}
