using System;

namespace DragonLegend.Whitebox
{
    public enum RecoveredSlotWinType {None=0,Big=1,Mega=2,Super=3}

    // Initial selection in CheckPlaySymbolAnim 23cce68, before any count/flight/popup.
    // These are presentation inputs, not a replacement for the remaining presentation.
    public sealed class RecoveredSymbolWinSelection
    {
        public readonly struct Cell
        {
            public readonly int Column,Row,SymbolId;
            public Cell(int column,int row,int symbol){Column=column;Row=row;SymbolId=symbol;}
        }
        private readonly RecoveredGameplayRules rules;
        private readonly Cell[] cells=new Cell[15];
        private readonly bool[] seen=new bool[15];
        public int Count {get;private set;}
        public float LineWin {get;private set;}
        public float TotalWin {get;private set;}
        public RecoveredSlotWinType BigWin {get;private set;}
        public bool HasReward=>TotalWin!=0;
        public bool RequestsLineSound=>HasReward&&LineWin>0;
        public RecoveredSymbolWinSelection(RecoveredGameplayRules gameplayRules)
            =>rules=gameplayRules??throw new ArgumentNullException(nameof(gameplayRules));
        public Cell At(int index)
        {
            if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
            return cells[index];
        }
        public void Capture(RecoveredSlotSettlement settlement,Func<int,int,int> readSymbol,float bonusCount,int currentBet)
        {
            LineWin=settlement.GetWinTotalLine();TotalWin=LineWin+bonusCount;BigWin=RecoveredSlotWinType.None;
            // Native returns before clearing the persistent visited-position list for zero total.
            if(!HasReward)return;
            BigWin=rules.GetBigWin(TotalWin,currentBet);Count=0;Array.Clear(seen,0,seen.Length);
            var lines=settlement.WinningLines;
            for(int i=0;i<lines.Count;i++) {
                var line=lines[i];
                for(int column=0;column<line.Count;column++) {
                    int row=line.GetRow(column),symbol=readSymbol(column,row),key=column*3+row;
                    if(seen[key])continue;
                    seen[key]=true;cells[Count++]=new Cell(column,row,symbol);
                }
            }
        }
    }
}
