using System;

namespace DragonLegend.Whitebox
{
    // Explicit original JakpotWinType numeric values; never used as string keys.
    public enum RecoveredJackpotType { None=0, Grand=1, Major=2, Minor=3 }

    // Predicates and task mutations from UIMainView's post-reel branches.
    // Call at the matching presentation stage, not while generating a result:
    // bonus animation -> Wild columns -> jackpot -> symbol animation -> bonus game
    // -> free game -> base end (ClickSpin MoveNext 0x23d1f30).
    public sealed class RecoveredRewardBranches
    {
        private readonly RecoveredGameplayRules rules;
        private readonly RecoveredPlayerProgress progress;
        // Original event key 7, handled by UIMainView.RefreshCashOutTask.
        public event Action<int,int> CashOutTaskRefreshRequested;
        public RecoveredRewardBranches(RecoveredGameplayRules rules, RecoveredPlayerProgress progress)
        {
            this.rules=rules ?? throw new ArgumentNullException(nameof(rules));
            this.progress=progress ?? throw new ArgumentNullException(nameof(progress));
        }

        // PlayFlyCoin completion 0x23c306c. Balance is read AFTER both callbacks:
        // either callback may change it. The presenter supplies the authored TopTitle reset.
        public void CompleteFlyCoin(float amount, Action onComplete, Action resetTopTitle)
        {
            onComplete?.Invoke();
            resetTopTitle();
            progress.SetGreenCount(progress.GreenCount + amount);
        }
        // CheckWild3 0x23d1520 counts ALL complete Wild columns, including gaps.
        public static int CountWildColumns(RecoveredSlotBoard board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            int count=0;
            for (int column=0; column<RecoveredSlotResultRules.Columns; column++)
                if (board.GetSymbol(column,0)==7 && board.GetSymbol(column,1)==7 && board.GetSymbol(column,2)==7)
                    count++;
            return count;
        }

        // CheckJackPot 0x23caa64: task 2 advances before the reward popup.
        // Popup award values and completion remain the presenter's responsibility.
        public RecoveredJackpotType CheckJackpot(int wildColumns)
        {
            if (wildColumns < 3) return RecoveredJackpotType.None;
            progress.SetTaskData(2,1);
            if (wildColumns == 3) return RecoveredJackpotType.Minor;
            if (wildColumns == 4) return RecoveredJackpotType.Major;
            return RecoveredJackpotType.Grand;
        }

        // CheckBonusGame 0x23c6d54: invoke after symbol animations, before free game.
        // True means data is saved and the presenter may start NPC/audio/transition.
        public bool CheckBonusGame() => progress.PrepareBonusGame();

        // CheckFreeGame 0x23c90bc: configured spins must be positive; task 3
        // advances before scatter animations, the intro popup and scene transition.
        public int CheckFreeGame(int scatterCount)
        {
            int count=rules.GetFreeSpins(scatterCount);
            if (count > 0)
            {
                progress.SetTaskData(3,1);
                CashOutTaskRefreshRequested?.Invoke(5,1);
                // Runtime GameData writes precede scatter animation and the intro.
                // No additional player save occurs in the native entry sequence.
                progress.GameSlotType = RecoveredSlotType.Free;
                progress.FreeSpinCount = count;
                progress.TotalFreeSpinWin = 0;
            }
            return count;
        }
    }
}
