using System;

namespace DragonLegend.Whitebox
{
    public readonly struct RecoveredBonusCoin
    {
        public readonly int Column, Row, Reward, CollectionCount;
        public RecoveredBonusCoin(int column, int row, int reward, int collectionCount)
        { Column = column; Row = row; Reward = reward; CollectionCount = collectionCount; }
    }

    // CheckPlayBonusAnim MoveNext 0x23cb808: column-major scan, one scaled delay
    // per coin (including the last). Animation callbacks run separately from this scan.
    public sealed class RecoveredBonusCoinSequence
    {
        private readonly RecoveredGameplayRules rules;
        private readonly RecoveredPlayerProgress progress;
        private readonly float coinInterval;
        private Func<int, int, int> readSymbol;
        private Action<RecoveredBonusCoin> present;
        private Action completed;
        private RecoveredReelWait wait;
        private int position;
        public bool IsRunning { get; private set; }
        public float TotalReward { get; private set; }
        public Exception Error { get; private set; }
        public RecoveredBonusCoinSequence(RecoveredGameplayRules rules, RecoveredPlayerProgress progress, float coinInterval)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            if (coinInterval < 0) throw new ArgumentOutOfRangeException(nameof(coinInterval));
            this.coinInterval = coinInterval;
        }
        public void Begin(Func<int, int, int> currentSymbol, Action<RecoveredBonusCoin> playCoin, Action onComplete)
        {
            if (IsRunning) throw new InvalidOperationException("Bonus coin scan is already running.");
            readSymbol = currentSymbol ?? throw new ArgumentNullException(nameof(currentSymbol));
            present = playCoin ?? throw new ArgumentNullException(nameof(playCoin));
            completed = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
            position = 0; TotalReward = 0; Error = null; IsRunning = true;
            Advance();
        }
        private void Advance()
        {
            wait = null;
            try {
                while (position < 15) {
                    int column = position / 3, row = position % 3;
                    position++;
                    if (readSymbol(column, row) != 9) continue;
                    int reward = rules.GetCoinReward();
                    TotalReward += reward;
                    int count = progress.CollectPresentedBonusCoin(column);
                    present(new RecoveredBonusCoin(column, row, reward, count));
                    // A GM change inside a presentation callback can explicitly cancel this scan.
                    if (!IsRunning) return;
                    wait = RecoveredReelWait.Delay(coinInterval, Advance, Fail);
                    return;
                }
                IsRunning = false;
                completed();
            } catch (Exception error) { Fail(error); }
        }
        private void Fail(Exception error) { Error = error; IsRunning = false; }
        public void CancelForProfileChange()
        {
            wait?.Cancel(); wait = null; IsRunning = false;
            readSymbol = null; present = null; completed = null;
        }
    }
}
