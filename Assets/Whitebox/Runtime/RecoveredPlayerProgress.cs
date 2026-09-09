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
        public IReadOnlyList<PlayerCashOutData> CashOutRecords => data.PlayerCashOutDatas;
        // Captured-record callback 23a278c, invoked only after the owning flow succeeds.
        // Do not reselect by id: the native closure retains the original record object.
        public void ApplyCashOutTaskStep(PlayerCashOutData record,int nextStep,int unixSeconds)
        {
            record.step=nextStep;record.count=0;record.time=unixSeconds;
            save();
        }
        // CashOutBottom.InitUI 23a0eb8: local conditions only; step 1000 enters SDK order handling.
        public RecoveredCashOutConditions GetCashOutConditions(int tier,int utcSeconds)
        {
            float target=rules.GetCashOutCash(tier);
            PlayerCashOutData record=null;
            for(int i=0;i<data.PlayerCashOutDatas.Count;i++)
                if(data.PlayerCashOutDatas[i].id==tier){record=data.PlayerCashOutDatas[i];break;}
            if(record==null)
                return new RecoveredCashOutConditions(false,false,GreenCount>=target,false,false,0,0,0,target-GreenCount);
            if(record.step==1000)
                return new RecoveredCashOutConditions(true,true,false,false,false,0,0,0,0);
            int count=record.count;
            int goal=record.isCashout?rules.GetSuccessTaskCount(tier,record.step):rules.GetFailTaskCount(tier,record.step);
            if(record.step==6){count=data.PlayerCollectDatas.Count;goal=rules.GetCollectInfoCount();}
            int remaining=unchecked(rules.GetWaitTime(tier,record.step)+record.time-utcSeconds);
            return new RecoveredCashOutConditions(true,false,true,count>=goal,remaining<=0,count,goal,remaining,0);
        }
        // UICashOutView.CheckItemKuang 23a714c. This differs from Main's prompt selection.
        public int FindCashOutWindowSelection(out bool hideFrame)
        {
            hideFrame=data.PlayerCashOutDatas.Count>=rules.GetCashOutCount();
            if(hideFrame)return -1;
            for(int tier=0;tier<rules.GetCashOutCount();tier++)
            {
                PlayerCashOutData record=null;
                for(int i=0;i<data.PlayerCashOutDatas.Count;i++)
                    if(data.PlayerCashOutDatas[i].id==tier){record=data.PlayerCashOutDatas[i];break;}
                if(record==null||record.step!=1000)return tier;
            }
            return -1;
        }
        // Main.RefreshCashOutTask 23ba71c; predicate 23c3044 compares step (+0x18), not type.
        public void RefreshCashOutTask(int step,int amount)
        {
            if(data.PlayerCashOutDatas!=null)
                for(int i=0;i<data.PlayerCashOutDatas.Count;i++)
                {
                    var record=data.PlayerCashOutDatas[i];
                    if(record.step==step)record.count=unchecked(record.count+amount);
                }
            save();
        }
        // Main.CheckBaseEnd 23c60b4..23c64fc: stop at the first absent record,
        // even if its prompt was already shown. Record state does not affect selection.
        public int PrepareCashOutPrompt()
        {
            for (int tier=0;tier<rules.GetCashOutCount();tier++)
            {
                bool found=false;
                for (int i=0;i<data.PlayerCashOutDatas.Count;i++)
                    if (data.PlayerCashOutDatas[i].id==tier) { found=true;break; }
                if (found) continue;
                if (data.CashOutTipIndexs==null)
                {
                    data.CashOutTipIndexs=new List<int>();
                    save();
                }
                // ARM fcmp GreenCount,target / b.lt also rejects unordered NaN.
                if (!(GreenCount>=rules.GetCashOutCash(tier)) || data.CashOutTipIndexs.Contains(tier)) return -1;
                data.CashOutTipIndexs.Add(tier);
                // The outer core continuation saves after the prompt, not here.
                return tier;
            }
            return -1;
        }
        public IReadOnlyList<PlayerCollectData> CollectRecords => data.PlayerCollectDatas;
        public int RandomIndex => data.RandomIndex;
        public PlayerCollectData GetPlayerCollectData(int id)
        {
            var records = data.PlayerCollectDatas;
            if (records == null) return null;
            for (int i = 0; i < records.Count; i++) if (records[i].id == id) return records[i];
            return null;
        }
        public event Action<float,float> GreenCountChanged;
        // Original GameData runtime flag; intentionally absent from PlayerData saves.
        public bool IsBonusGame { get; set; }
        // GameData +0x5a, runtime only. The jackpot view reads it again on click.
        public bool IsFirstFreeReward { get; set; }
        public RecoveredSlotType GameSlotType { get; set; }
        public int FreeSpinCount { get; set; }
        public float TotalFreeSpinWin { get; set; }
        // GameData 0x4c/0x50/0x54, runtime values written by the jackpot meters.
        // These are independent of the credited balance and absent from PlayerData.
        public float GrandJackPotReward { get; set; }
        public float MajorJackPotReward { get; set; }
        public float MiniJackPotReward { get; set; }
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
        // 0x236f48c: first matching ID, new count is always 1. Even ignored writes save.
        public void SetCollectData(int id, int increment)
        {
            var records = data.PlayerCollectDatas;
            if (records != null)
            {
                PlayerCollectData record = null;
                for (int i = 0; i < records.Count; i++)
                    if (records[i].id == id) { record = records[i]; break; }
                if (record == null) records.Add(new PlayerCollectData {id=id,count=1,isRecieve=false});
                else if (!record.isRecieve) record.count = unchecked(record.count + increment);
            }
            save();
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
