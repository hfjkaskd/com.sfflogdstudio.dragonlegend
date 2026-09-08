using System;
using System.Collections.Generic;
using DragonLegend.Whitebox.Recovered;

namespace DragonLegend.Whitebox
{
    public enum RecoveredSlotType { Base = 0, Free = 1 }

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
        public float GreenCount => data.GreenCount;
        public IReadOnlyList<int> BonusArea => data.BonusArea;
        public event Action<float,float> GreenCountChanged;
        // Original GameData runtime flag; intentionally absent from PlayerData saves.
        public bool IsBonusGame { get; set; }
        public RecoveredSlotType GameSlotType { get; set; }
        public int FreeSpinCount { get; set; }
        public float TotalFreeSpinWin { get; set; }
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

        // GameData.set_GreenCount 0x236d788: notify before mutation, then two saves.
        public void SetGreenCount(float value)
        {
            GreenCountChanged?.Invoke(data.GreenCount,value);
            data.GreenCount = value;
            // CheckGreenCount 0x236d884 literal constants, verified against the ELF.
            // Native b.lt also passes unordered (NaN); preserve that comparison.
            int milestone = data.GreenCountLog;
            if ((milestone == 0 && !(value < 20000f)) ||
                (milestone == 1 && !(value < 40000f)) ||
                (milestone == 2 && !(value < 60000f)) ||
                (milestone == 3 && !(value < 80000f)))
                data.GreenCountLog++;
            // SDK tracking omitted; retain milestone state and persistence behavior.
            save(); // CheckGreenCount always saves, even without a new milestone.
            save(); // The caller setter saves again.
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

        // CheckPlayBonusAnim directly increments PlayerData, without a cap or save.
        // This is distinct from GameData.SetBonusArea below.
        internal int CollectPresentedBonusCoin(int reel)
        {
            return data.BonusArea[reel] = unchecked(data.BonusArea[reel] + 1);
        }
        // GameData.SetBonusArea 0x236f868: each column stops accepting hits at 2.
        // Existing values above 2 are retained, negative values increment without clamping.
        // The original emits no UI event here and does not save on the early return.
        public void SetBonusArea(int reel)
        {
            int value = data.BonusArea[reel];
            if (value > 1) return;
            data.BonusArea[reel] = unchecked(value + 1);
            save();
        }
        // Data stage of UIMainView.CheckBonusGame (0x23c6d54), before NPC/audio/transition.
        // Predicate 0x23bf8d8 is x > 1. Equality of filtered and total counts means
        // even an empty (but non-null) area triggers, and a previously set flag wins.
        internal bool PrepareBonusGame()
        {
            var area = data.BonusArea;
            int complete = 0;
            for (int i = 0; i < area.Count; i++)
                if (area[i] > 1) complete++;
            if (complete == area.Count) IsBonusGame = true;
            if (!IsBonusGame) return false;
            SetTaskData(5, 1); // Its save observes the OLD area and the set runtime flag.
            IsBonusGame = false;
            data.BonusArea = new List<int>(5) { 0, 0, 0, 0, 0 };
            save();
            return true;
        }
        // 0x236eb10: first occurrence is always 1, regardless of increment.
        // Claimed records and a null list still save without changing progress.
        public void SetTaskData(int id, int increment)
        {
            var tasks = data.PlayerTaskDatas;
            if (tasks != null)
            {
                PlayerTaskData task = null;
                for (int i = 0; i < tasks.Count; i++)
                    if (tasks[i].id == id) { task = tasks[i]; break; }
                if (task == null) tasks.Add(new PlayerTaskData {id=id,count=1,isRecieve=false});
                else if (!task.isRecieve)
                {
                    task.count = unchecked(task.count + increment);
                    var infos = rules.GetTaskInfos();
                    RecoveredTaskInfo info = null;
                    for (int i = 0; i < infos.Count; i++)
                        if (infos[i].id == id) { info=infos[i]; break; }
                    // Original dereferences Find's result; no unknown-ID fallback.
                    int maximum = info.taskAmount;
                    task.count = task.count < 0 ? 0 : Math.Min(task.count, maximum);
                }
            }
            save();
        }
    }
}
