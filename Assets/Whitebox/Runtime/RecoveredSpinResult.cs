using System;
using System.Collections.Generic;

namespace DragonLegend.Whitebox
{
    // InitGameResult 0x2383290. Does not debit spins, grant balances, or open UI:
    // those belong to the original caller/state machine, still to be connected.
    public sealed class RecoveredSpinResult
    {
        private readonly RecoveredGameplayRules rules;
        private readonly List<int> columns = new List<int>(5);
        private int stage;
        private int bet;
        private Action<int> startCall;
        private Action<int> scatterCall;
        public RecoveredSlotBoard Board { get; }
        public RecoveredSlotSettlement Settlement { get; }
        public int WildCounter { get; private set; }
        public int BonusCounter { get; private set; }
        public int ScatterCount { get; private set; }
        public bool FirstFreeReward { get; private set; }
        public bool ForceFreeSpin { get; set; }
        public bool IsGenerating => stage != 0;

        public RecoveredSpinResult(RecoveredGameplayRules gameplayRules, RecoveredSlotSettlement settlement)
        {
            rules = gameplayRules ?? throw new ArgumentNullException(nameof(gameplayRules));
            Settlement = settlement ?? throw new ArgumentNullException(nameof(settlement));
            Board = new RecoveredSlotBoard(rules);
        }

        public void Begin(bool isGuide, int currentBet, int moreWild, IReadOnlyList<int> bonusAreaProgress,
            Action<int> onStart = null, Action<int> onScatter = null)
        {
            if (IsGenerating) throw new InvalidOperationException("A result is already being generated.");
            if (bonusAreaProgress == null) throw new ArgumentNullException(nameof(bonusAreaProgress));
            bet = currentBet;
            startCall = onStart;
            scatterCall = onScatter;
            ScatterCount = 0;
            WildCounter = unchecked(WildCounter + 1);
            BonusCounter = unchecked(BonusCounter + 1);
            FirstFreeReward = isGuide;
            if (isGuide) WildCounter = rules.GetWildSpinCD();
            Board.FillBase();
            if (WildCounter >= rules.GetWildSpinCD())
            {
                WildCounter = 0;
                Board.ApplyGuaranteedWilds();
                stage = 3; // original jumps directly to settlement, skipping Bonus/Scatter
                return;
            }
            Board.ApplyRegularWilds(moreWild);
            BonusCounter = unchecked(BonusCounter + 1); // second increment in ordinary branch
            columns.Clear();
            if (BonusCounter < rules.GetMinSpin())
            {
                int count = rules.GetCoinSpinAmount();
                if (count > 0) Board.BeginSingleSymbol(count, columns, 9);
            }
            else
            {
                int count = rules.GetCoinSpinAmountWin();
                BonusCounter = 0;
                for (int i = 0; i < bonusAreaProgress.Count; i++)
                    if (bonusAreaProgress[i] <= 1) columns.Add(i);
                if (count > 0) Board.BeginGuaranteedBonus(count, columns);
            }
            stage = 1;
        }

        // Advances at most one random-retry attempt; caller can budget work per frame.
        public bool Step()
        {
            if (stage == 0) return false;
            if (Board.IsPlacingSingleSymbol && Board.StepSingleSymbol()) return true;
            if (stage == 1)
            {
                if (ForceFreeSpin) { ForceFreeSpin = false; ScatterCount = UnityEngine.Random.Range(3,6); }
                else ScatterCount = rules.GetSpinScatterAmount();
                columns.Clear();
                if (ScatterCount > 0) Board.BeginSingleSymbol(ScatterCount, columns, 10);
                stage = 2;
                return true;
            }
            Board.Settle(Settlement, bet);
            int speed = Board.SpeedRoll(ScatterCount);
            var start = startCall; var scatter = scatterCall;
            startCall = null; scatterCall = null;
            stage = 0;
            start?.Invoke(speed);
            scatter?.Invoke(ScatterCount);
            return false;
        }
    }
}
