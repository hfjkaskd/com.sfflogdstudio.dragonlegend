using System;

namespace DragonLegend.Whitebox
{
    // GameData property behavior; caller supplies the persistence operation so these
    // partial fields are never serialized over the full original player record.
    public sealed class RecoveredPlayerProgress
    {
        private readonly RecoveredGameplayRules rules;
        private readonly Action save;
        public int Level { get; private set; }
        public float Experience { get; private set; }
        public int SpinCount { get; private set; }
        public int MoreWild { get; private set; }
        public event Action<int> SpinCountChanged;
        public event Action<int,float,float> LevelExperienceChanged;
        public event Action ReviewRequested;
        public event Action MoreWildChanged;

        public RecoveredPlayerProgress(RecoveredGameplayRules gameplayRules, Action savePlayer,
            int level, float experience, int spinCount, int moreWild)
        {
            rules = gameplayRules ?? throw new ArgumentNullException(nameof(gameplayRules));
            save = savePlayer ?? throw new ArgumentNullException(nameof(savePlayer));
            Level = level; Experience = experience; SpinCount = spinCount; MoreWild = moreWild;
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
    }
}
