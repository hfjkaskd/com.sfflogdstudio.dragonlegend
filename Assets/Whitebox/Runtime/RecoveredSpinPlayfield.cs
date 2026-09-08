using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredSpinPlayfield : MonoBehaviour
    {
        [SerializeField] private RecoveredSpinButton spinButton;
        [SerializeField] private RecoveredBaseReelController reels;
        [SerializeField] private RecoveredSymbolCatalog symbols;
        [SerializeField] private float rewardDelay;
        [SerializeField] private float bonusCoinInterval;
        [SerializeField] private RecoveredBonusCollection bonusCollection;
        public RecoveredBonusCollection BonusCollection => bonusCollection;
        [SerializeField] private RecoveredCoinStopPresenter coinStops;
        public RecoveredCoinStopPresenter CoinStops => coinStops;
        private RecoveredReelWait rewardWait;
        private RecoveredSpinEntry entry;
        private RecoveredSpinResult result;
        private RecoveredGameplayRules rules;
        private readonly List<int> bets = new List<int>(5);
        private readonly int[][] columns = { new int[3], new int[3], new int[3], new int[3], new int[3] };
        public int Bet { get; private set; }
        public bool IsBusy { get; private set; }
        public bool AwaitingRewards { get; private set; }
        public Exception Error { get; private set; }
        public RecoveredBonusCoinSequence BonusCoins { get; private set; }
        public event Action<RecoveredBonusCoin> BonusCoinPresentationRequested;
        public event Action WildColumnsCheckRequested;
        public RecoveredSpinButton SpinButton => spinButton;
        public RecoveredBaseReelController Reels => reels;
        public event Action RewardSequenceRequested;
        public void Bind(RecoveredSpinEntry spinEntry, RecoveredSpinResult spinResult,
            RecoveredPlayerProgress progress, RecoveredGameplayRules gameplayRules, bool isA)
        {
            Unbind(); entry = spinEntry; result = spinResult; rules = gameplayRules;
            BonusCoins = new RecoveredBonusCoinSequence(rules, progress, bonusCoinInterval);
            if (bonusCollection != null) bonusCollection.Initialize(progress.BonusArea);
            rules.GetBet(isA, progress.Level, bets);
            // GameData.Init resets Bet=0; SetBet only assigns list[0] for the normal branch.
            Bet = isA ? 0 : bets[0];
            reels.Initialize(symbols);
            if(coinStops!=null)coinStops.Bind(reels);
            entry.StartVisualsRequested += Started;
            reels.ReelsStopped += Stopped;
            spinButton.Button.onClick.AddListener(Click);
        }
        private void Click()
        {
            if (entry == null) return;
            if (entry.TryBegin(IsBusy, Bet, rules.GetConfigType(), DateTimeOffset.UtcNow.ToUnixTimeSeconds(), ResultReady)) {
                // InitGameResult 0x2383290 is synchronous. Preserve all random attempts and
                // invoke the first reel's startup before this click handler returns.
                while (result.IsGenerating) result.Step();
            }
        }
        private void Started() { IsBusy = true; AwaitingRewards = false; Error = null; spinButton.PlayAcceptedClick(); }
        private void ResultReady(int index) => reels.Begin(index, ReadColumn);
        private IReadOnlyList<int> ReadColumn(int index)
        {
            var column = columns[index];
            for (int row = 0; row < column.Length; row++) column[row] = result.Board.GetSymbol(index, row);
            return column;
        }
        private void Stopped()
        {
            // ClickSpin 0x23d237c waits 0.5 scaled seconds after all reels stop,
            // before CheckPlayBonusAnim and the remaining awaited reward stages.
            rewardWait = RecoveredReelWait.Delay(rewardDelay, BeginRewards, error => Error = error);
        }
        private void BeginRewards()
        {
            rewardWait = null;
            AwaitingRewards = true;
            RewardSequenceRequested?.Invoke();
            if (entry != null)
                BonusCoins.Begin(result.Board.GetSymbol, coin => BonusCoinPresentationRequested?.Invoke(coin),
                    () => WildColumnsCheckRequested?.Invoke());
        }
        // Called by the eventual CheckBaseEnd completion, never by a reel stop callback.
        public void CompleteBaseRound()
        {
            if (!AwaitingRewards) throw new InvalidOperationException("The reel sequence has not reached rewards.");
            AwaitingRewards = false; IsBusy = false;
        }
        public void Unbind()
        {
            rewardWait?.Cancel(); rewardWait = null;
            BonusCoins?.CancelForProfileChange(); BonusCoins = null;
            if(coinStops!=null)coinStops.Unbind();
            spinButton.Button.onClick.RemoveListener(Click);
            if (entry != null) entry.StartVisualsRequested -= Started;
            reels.ReelsStopped -= Stopped;
            reels.AbortForProfileChange();
            entry = null; result = null; rules = null; IsBusy = false; AwaitingRewards = false; Error = null;
        }
        private void OnDestroy() => Unbind();
    }
}
