using System;
using System.Collections.Generic;

namespace DragonLegend.Whitebox
{
    public enum RecoveredBonusType { Reward=0, Zhao=1, Cai=2, Jin=3, Bao=4 }

    // Data portion of UIBonusView. The presenter owns Button/advertisement gates,
    // animation completion and window exit; accepting a card never credits cash.
    public sealed class RecoveredBonusRound
    {
        // Original ctor 239b698 / OnBeforeShow 23997b4 insertion order matters:
        // a Cai/Bao goes to Grand first, then Minor/Major after Grand used that type.
        private static readonly RecoveredBonusType[][] Patterns = {
            new[] { RecoveredBonusType.Zhao, RecoveredBonusType.Cai, RecoveredBonusType.Jin, RecoveredBonusType.Bao },
            new[] { RecoveredBonusType.Bao, RecoveredBonusType.Bao, RecoveredBonusType.Bao },
            new[] { RecoveredBonusType.Cai, RecoveredBonusType.Cai, RecoveredBonusType.Cai }
        };
        private readonly List<RecoveredBonusType> rewards = new List<RecoveredBonusType>();
        private readonly List<int> clicked = new List<int>();
        private readonly bool[][] used = { new bool[4], new bool[3], new bool[3] };
        public int Count => rewards.Count;
        public int ClickedCount => clicked.Count;
        public RecoveredBonusType GetCard(int index) => rewards[index];
        public bool WasClicked(int index) => clicked.Contains(index);

        public void Initialize(RecoveredGameplayRules rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            rewards.Clear();
            clicked.Clear();
            for (int i=0; i<used.Length; i++) Array.Clear(used[i], 0, used[i].Length);
            var counts = rules.GetBonusAllReward();
            // InitBonusResult 2399e84: expand Zhao,Cai,Jin,Bao,Reward in that order.
            for (int type=0; type<5; type++)
                for (int j=0; j<counts[type]; j++)
                    rewards.Add(type == 4 ? RecoveredBonusType.Reward : (RecoveredBonusType)(type+1));
            // Original forward shuffle includes the final Random.Range(n-1,n).
            for (int i=0; i<rewards.Count; i++) {
                int other = UnityEngine.Random.Range(i, rewards.Count);
                var value = rewards[i]; rewards[i] = rewards[other]; rewards[other] = value;
            }
        }

        // ClickBonus 239afc4 / callback 239bfdc / CheckJackPotReward 239c4bc.
        // Call only after free selection or successful rewarded-ad completion.
        public bool TryReveal(int index, out Reveal result)
        {
            result = default;
            if (clicked.Contains(index)) return false;
            var value = rewards[index];
            int group=-1, target=-1;
            for (int i=0; i<Patterns.Length && group<0; i++)
                for (int j=0; j<Patterns[i].Length; j++)
                    if (!used[i][j] && Patterns[i][j] == value) { group=i; target=j; break; }
            clicked.Add(index);
            bool complete=false;
            if (group>=0) {
                used[group][target]=true;
                complete=true;
                for (int j=0; j<used[group].Length; j++)
                    if (!used[group][j]) { complete=false; break; }
            }
            result = new Reveal(value, (RecoveredJackpotType)(group+1), target, complete);
            return true;
        }

        public readonly struct Reveal
        {
            public readonly RecoveredBonusType type;
            public readonly RecoveredJackpotType jackpot;
            public readonly int targetIndex;
            public readonly bool completesJackpot;
            public Reveal(RecoveredBonusType type, RecoveredJackpotType jackpot, int targetIndex, bool completesJackpot)
            { this.type=type; this.jackpot=jackpot; this.targetIndex=targetIndex; this.completesJackpot=completesJackpot; }
        }
    }
}
