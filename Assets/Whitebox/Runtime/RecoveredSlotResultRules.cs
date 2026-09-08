using System;
using System.Collections.Generic;

namespace DragonLegend.Whitebox
{
    // Native SlotGameResult helpers, verified against ARM64 bodies in mumu-current.
    // These are symbol IDs and board dimensions from the original game protocol.
    public static class RecoveredSlotResultRules
    {
        public const int Columns = 5;
        public const int Rows = 3;
        public const int Wild = 7;
        public const int Scatter = 10;

        // 0x238626c, 0x238671c, 0x23867c8. Zero is the original helper sentinel,
        // including when the second scatter is in column zero; preserve that edge.
        public static int SpeedRoll(int[,] result, int scatterCount)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.GetLength(0) != Columns || result.GetLength(1) != Rows)
                throw new ArgumentException("Expected the original 5 by 3 board.", nameof(result));
            int scatterColumn = 0;
            int seen = 0;
            if (scatterCount >= 2)
            {
                for (int col = 0; col < Columns && seen < 2; col++)
                    for (int row = 0; row < Rows; row++)
                        if (result[col, row] == Scatter && ++seen == 2)
                        {
                            scatterColumn = col;
                            break;
                        }
            }
            int wildColumn = 0;
            int wildColumns = 0;
            for (int col = 0; col < Columns; col++)
                if (result[col, 0] == Wild && result[col, 1] == Wild && result[col, 2] == Wild)
                    if (++wildColumns == 2) wildColumn = col;
            if (scatterColumn == 0) return wildColumn == 0 ? -1 : wildColumn;
            return wildColumn == 0 ? scatterColumn : Math.Min(scatterColumn, wildColumn);
        }

        // 0x2386414: only a consecutive prefix counts, not all wilds in a line.
        public static bool HasWildEqualMoreThanThree(IReadOnlyList<int> symbols, out int count)
        {
            if (symbols == null) throw new ArgumentNullException(nameof(symbols));
            count = 0;
            while (count < symbols.Count && symbols[count] == Wild) count++;
            if (count >= 3) return true;
            count = 0;
            return false;
        }

        // 0x2386504: native getter also writes the minimum positive award back.
        public static float GetWinTotalLine(ref float lineWinCount)
        {
            if (lineWinCount > 0f && lineWinCount < 1f) lineWinCount = 1f;
            return lineWinCount;
        }
    }
}
