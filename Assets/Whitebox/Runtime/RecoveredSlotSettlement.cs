using System;
using System.Collections.Generic;

namespace DragonLegend.Whitebox
{
    public struct RecoveredWinLine
    {
        public int SymbolId { get; internal set; }
        public int Count { get; internal set; }
        internal int PackedRows;
        internal bool Extended;
        internal bool Collected;
        public int GetRow(int column)
        {
            if (column < 0 || column >= Count) throw new ArgumentOutOfRangeException(nameof(column));
            return (PackedRows >> (column * 2)) & 3;
        }
    }

    // SlotGameResult.WinTotalLine 0x23851a0, predicate 0x2386bd8.
    // Same column/row/path traversal and matching branches, with reusable storage.
    // At most 3+9+27+81+243 path nodes exist on the native 5x3 board.
    public sealed class RecoveredSlotSettlement
    {
        private readonly RecoveredGameplayRules rules;
        private readonly RecoveredWinLine[] nodes = new RecoveredWinLine[363];
        private readonly int[] previous = new int[243];
        private readonly int[] next = new int[243];
        private readonly int[] collected = new int[363];
        private readonly List<RecoveredWinLine> winningLines = new List<RecoveredWinLine>(363);
        private int nodeCount;
        private int collectedCount;
        private float lineWinCount;
        // Valid until the next Evaluate call. No per-spin copy is made.
        public IReadOnlyList<RecoveredWinLine> WinningLines => winningLines;
        public float RawAward => lineWinCount;
        public float GetWinTotalLine() => RecoveredSlotResultRules.GetWinTotalLine(ref lineWinCount);

        public RecoveredSlotSettlement(RecoveredGameplayRules gameplayRules)
        {
            rules = gameplayRules ?? throw new ArgumentNullException(nameof(gameplayRules));
        }

        public void Evaluate(int[,] board, int bet)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (board.GetLength(0) != 5 || board.GetLength(1) != 3)
                throw new ArgumentException("Expected the original 5 by 3 board.", nameof(board));
            nodeCount = collectedCount = 0;
            winningLines.Clear();
            lineWinCount = 0;
            int previousCount = 0;
            int nextCount = 0;
            for (int col = 0; col < 5; col++)
            {
                nextCount = 0;
                for (int row = 0; row < 3; row++)
                {
                    int symbol = board[col, row];
                    if (symbol >= 8) continue;
                    if (col == 0)
                    {
                        nodes[nodeCount] = new RecoveredWinLine {SymbolId = symbol, Count = 1, PackedRows = row};
                        next[nextCount++] = nodeCount++;
                        continue;
                    }
                    for (int path = 0; path < previousCount; path++)
                    {
                        int index = previous[path];
                        var parent = nodes[index];
                        if (parent.SymbolId == 7 || parent.SymbolId == symbol || symbol == 7)
                        {
                            nodes[index].Extended = true;
                            nodes[nodeCount] = new RecoveredWinLine {
                                SymbolId = parent.SymbolId == 7 ? symbol : parent.SymbolId,
                                Count = parent.Count + 1,
                                PackedRows = parent.PackedRows | (row << (col * 2))
                            };
                            next[nextCount++] = nodeCount++;
                        }
                        else if (parent.Count >= 3 && !parent.Collected)
                        {
                            nodes[index].Collected = true;
                            collected[collectedCount++] = index;
                        }
                    }
                }
                if (col != 4)
                {
                    Array.Copy(next, previous, nextCount);
                    previousCount = nextCount;
                }
            }
            for (int i = 0; i < nextCount; i++) collected[collectedCount++] = next[i];
            int payTotal = 0;
            for (int i = 0; i < collectedCount; i++)
            {
                var line = nodes[collected[i]];
                if (line.Extended) continue;
                winningLines.Add(line);
                int pay = rules.GetPay(line.SymbolId, line.Count);
                int leadingWilds = 0;
                while (leadingWilds < line.Count && board[leadingWilds, line.GetRow(leadingWilds)] == 7)
                    leadingWilds++;
                // Original replaces the pay, even if the Wild pay is smaller.
                if (leadingWilds >= 3) pay = rules.GetPay(7, leadingWilds);
                payTotal = unchecked(payTotal + pay);
            }
            // Native mul w then scvtf then fdiv: do not promote multiplication to float.
            lineWinCount = (float)unchecked(bet * payTotal) / rules.GetLines();
        }
    }
}
