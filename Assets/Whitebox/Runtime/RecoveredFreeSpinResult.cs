using System;
using System.Collections.Generic;

namespace DragonLegend.Whitebox
{
    // FreeSlotGameResult.InitFreeGameResult 0x238265c and CheckSingleSymbol 0x2382b24.
    // Symbol IDs are supplied from the authored free symbol definitions in original order.
    public sealed class RecoveredFreeSpinResult
    {
        private readonly RecoveredGameplayRules rules;
        private readonly int[,] symbols = new int[5,3];
        private readonly bool[,] reserved = new bool[5,3];
        private readonly int[] rows = new int[3];
        private readonly List<int> ballTypes = new List<int>(15);
        private int stage;
        private int remaining;
        private Action complete;
        public int CoinAmount { get; private set; }
        public int BallAmount { get; private set; }
        public bool IsGenerating => stage != 0;
        public int GetSymbol(int column, int row) => symbols[column,row];
        public int GetBall(int index) => ballTypes[index];
        public int GeneratedBallCount => ballTypes.Count;

        public RecoveredFreeSpinResult(RecoveredGameplayRules rules)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public void Begin(IReadOnlyList<int> symbolIds, Action onComplete = null)
        {
            if (IsGenerating) throw new InvalidOperationException("A free result is already being generated.");
            if (symbolIds == null) throw new ArgumentNullException(nameof(symbolIds));
            CoinAmount = rules.GetFreeCoinAmount();
            BallAmount = rules.GetFreeBallAmount();
            for (int column = 0; column < 5; column++)
                for (int row = 0; row < 3; row++)
                    symbols[column,row] = symbolIds[UnityEngine.Random.Range(0,symbolIds.Count)];
            Array.Clear(reserved,0,reserved.Length);
            complete = onComplete;
            remaining = CoinAmount;
            stage = 1;
        }

        // One original column retry per step; callers budget steps per frame.
        // A full board with excessive configured placements stays pending, never silently clamps.
        public bool Step()
        {
            if (stage == 0) return false;
            if (remaining <= 0 && stage == 1) { stage = 2; remaining = BallAmount; }
            if (remaining > 0)
            {
                int column = UnityEngine.Random.Range(0,5);
                int count = 0;
                for (int row = 0; row < 3; row++)
                    if (!reserved[column,row]) rows[count++] = row;
                if (count == 0) return true;
                int selectedRow = rows[UnityEngine.Random.Range(0,count)];
                symbols[column,selectedRow] = stage == 1 ? 9 : 11;
                reserved[column,selectedRow] = true;
                remaining--;
                return true;
            }
            // Native ball list is rebuilt only after both placement passes finish.
            ballTypes.Clear();
            for (int column = 0; column < 5; column++)
                for (int row = 0; row < 3; row++)
                    if (symbols[column,row] == 11) ballTypes.Add(rules.GetFreeBallType());
            stage = 0;
            var callback = complete;
            complete = null;
            callback?.Invoke();
            return false;
        }
    }
}