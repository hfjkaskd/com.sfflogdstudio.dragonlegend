using System;

namespace DragonLegend.Whitebox
{
    // Board construction stages recovered from InitGameResult (0x2383290),
    // ChangeWild (0x2383a34), and ReelsCheckWild (0x2383f2c).
    // Bonus/Scatter and the complete spin orchestration are separate pending stages.
    public sealed class RecoveredSlotBoard
    {
        private readonly RecoveredGameplayRules rules;
        private readonly int[,] symbols = new int[5,3];
        private readonly bool[,] reserved = new bool[5,3];
        private readonly int[] order = new int[5];
        private readonly int[] keys = new int[5];
        public int GetSymbol(int column, int row) => symbols[column,row];
        public bool IsReserved(int column, int row) => reserved[column,row];

        public RecoveredSlotBoard(RecoveredGameplayRules gameplayRules)
        {
            rules = gameplayRules ?? throw new ArgumentNullException(nameof(gameplayRules));
        }

        public void FillBase()
        {
            // Preserve column-major Unity.Random consumption.
            for (int col = 0; col < 5; col++)
                for (int row = 0; row < 3; row++)
                    symbols[col,row] = rules.RandomReelSymbol(col);
        }

        public void ApplyRegularWilds(int moreWild)
        {
            // Original clears insteadList before this call, then creates all five keys.
            Array.Clear(reserved, 0, reserved.Length);
            for (int col = 0; col < 5; col++)
            {
                int count = moreWild > 0 ? rules.RandomMoreWildSymbol(col) : rules.RandomWildSymbol(col);
                if (count < 1) continue;
                SelectRandomOrder(3, new Random());
                int taken = Math.Min(count, 3);
                for (int i = 0; i < taken; i++) reserved[col,order[i]] = true;
                for (int row = 0; row < 3; row++)
                    if (reserved[col,row]) symbols[col,row] = RecoveredSlotResultRules.Wild;
            }
        }

        public void ApplyGuaranteedWilds()
        {
            ApplyGuaranteedWildIndex(rules.RandomWildCount());
        }

        // Index 2 retains the original full [0..4] list; unknown indexes use an empty list.
        // Supplying a seeded Random allows deterministic replay of this stage.
        public void ApplyGuaranteedWildIndex(int index, Random random = null)
        {
            int count = index == 0 ? 4 : index == 1 ? 3 : index == 2 ? 5 : 0;
            if (count == 0) return;
            if (count == 5)
                for (int col = 0; col < 5; col++) order[col] = col;
            else SelectRandomOrder(5, random ?? new Random());
            for (int i = 0; i < count; i++)
                for (int row = 0; row < 3; row++) symbols[order[i],row] = RecoveredSlotResultRules.Wild;
            // Native ChangeWild does not update insteadList; preserve that distinction.
        }

        private void SelectRandomOrder(int length, Random random)
        {
            // Equivalent to original stable OrderBy(_ => random.Next()).Take(n).
            // Generate keys in source order before sorting. Equal keys retain source order.
            for (int i = 0; i < length; i++) { keys[i] = random.Next(); order[i] = i; }
            for (int i = 1; i < length; i++)
            {
                int item = order[i];
                int j = i - 1;
                while (j >= 0 && keys[order[j]] > keys[item])
                { order[j + 1] = order[j]; j--; }
                order[j + 1] = item;
            }
        }

        public void Settle(RecoveredSlotSettlement settlement, int bet)
        {
            if (settlement == null) throw new ArgumentNullException(nameof(settlement));
            settlement.Evaluate(symbols, bet);
        }
        public int SpeedRoll(int scatterCount) => RecoveredSlotResultRules.SpeedRoll(symbols, scatterCount);
    }
}
