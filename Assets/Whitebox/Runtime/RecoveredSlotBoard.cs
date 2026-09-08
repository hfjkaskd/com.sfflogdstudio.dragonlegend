using System;
using System.Collections.Generic;

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
        private static readonly Func<int, int, int> UnityRange = UnityEngine.Random.Range;
        private readonly List<int> candidateRows = new List<int>(15);
        private List<int> placementColumns;
        private int placementRemaining;
        private int placementSymbol;
        private bool placementActive;
        private bool placementAttemptStarted;
        public bool IsPlacingSingleSymbol => placementActive;
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

        // CheckSingleSymbol 0x2384d2c. Caller-owned column list is updated by the
        // original routine; existing entries forbid columns, irrespective of board contents.
        public void BeginSingleSymbol(int count, List<int> columns, int symbol)
        {
            if (placementActive) throw new InvalidOperationException("Finish the current placement first.");
            placementColumns = columns ?? throw new ArgumentNullException(nameof(columns));
            placementRemaining = count;
            placementSymbol = symbol;
            placementAttemptStarted = false;
            candidateRows.Clear();
            placementActive = true;
        }

        public bool StepSingleSymbol() => StepSingleSymbol(UnityRange);

        // One native column attempt per call. False means complete. The spin driver
        // can yield between attempts without changing the random sequence or result.
        public bool StepSingleSymbol(Func<int, int, int> range)
        {
            if (!placementActive) return false;
            if (range == null) throw new ArgumentNullException(nameof(range));
            if (!placementAttemptStarted)
            {
                int reservedCount = 0;
                for (int c = 0; c < 5; c++) for (int r = 0; r < 3; r++)
                    if (reserved[c,r]) reservedCount++;
                if (reservedCount == 15 || placementColumns.Count == 5 || --placementRemaining < 0)
                {
                    placementActive = false;
                    placementColumns = null;
                    return false;
                }
                candidateRows.Clear();
                placementAttemptStarted = true;
            }
            int column = range(0, 5);
            for (int row = 0; row < 3; row++)
                if (!reserved[column,row]) candidateRows.Add(row);
            // Native 0x2384f30 appends on retry: intentionally DO NOT clear here.
            if (placementColumns.Contains(column) || candidateRows.Count == 0) return true;
            int selectedRow = candidateRows[range(0, candidateRows.Count)];
            symbols[column,selectedRow] = placementSymbol;
            reserved[column,selectedRow] = true;
            placementColumns.Add(column);
            placementAttemptStarted = false;
            return true;
        }
    }
}
