using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredSpinPlayfield : MonoBehaviour
    {
        [SerializeField] private RecoveredNpcPresentation npc;
        [SerializeField] private RecoveredMainComposition composition;
        public RecoveredMainComposition Composition => composition;
        [SerializeField] private RecoveredFreeBottom freeBottom;
        public RecoveredFreeBottom FreeBottom => freeBottom;
        [SerializeField] private RecoveredMainModeView modeView;
        public RecoveredMainModeView ModeView=>modeView;
        public RecoveredSymbolCatalog Symbols=>symbols;
        public RecoveredNpcPresentation Npc=>npc;
        [SerializeField] private RecoveredSpinButton spinButton;
        [SerializeField] private RecoveredSpinRecoveryView spinRecovery;
        public RecoveredSpinRecoveryView SpinRecovery=>spinRecovery;
        [SerializeField] private RecoveredBaseReelController reels;
        [SerializeField] private RecoveredSymbolCatalog symbols;
        [SerializeField] private float rewardDelay;
        [SerializeField] private float bonusCoinInterval;
        [SerializeField] private float wildColumnInterval;
        [SerializeField] private RecoveredWildPresenter wilds;
        [SerializeField] private RecoveredSymbolWinPresenter symbolEffects;
        public RecoveredSymbolWinPresenter SymbolEffects=>symbolEffects;
        [SerializeField] private RecoveredJackpotMeters jackpotMeters;
        [SerializeField] private RecoveredJackpotPopup jackpotPopup;
        [SerializeField] private float jackpotDelay;
        private RecoveredPlayerProgress player;
        private IAdFacade ads;
        private bool isA;
        private int language;
        public RecoveredJackpotPopup JackpotPopup=>jackpotPopup;
        [SerializeField] private RecoveredBigWinPopup bigWinPopup;
        [SerializeField] private RecoveredBigWinSequence bigWinSequence;
        public RecoveredBigWinPopup BigWinPopup=>bigWinPopup;
        public RecoveredBigWinSequence BigWinSequence=>bigWinSequence;
        public RecoveredJackpotSequence JackpotSequence {get;private set;}
        public event Action SymbolAnimationsRequested,SymbolAmountReady,SymbolSequenceCompleted,BigWinBranchEntered;
        [SerializeField] private RecoveredOrdinaryWinSequence ordinaryWin;
        public RecoveredOrdinaryWinSequence OrdinaryWin=>ordinaryWin;
        [SerializeField] private RecoveredSymbolWinAmount symbolAmount;
        public RecoveredSymbolWinAmount SymbolAmount=>symbolAmount;
        public RecoveredSymbolWinSelection SymbolWin {get;private set;}
        public event Action PauseMusicRequested,StopSound1Requested,ResumeMusicRequested,HideWheelRequested;
        public event Action<string> SoundRequested,Sound1Requested;
        public event Action<float,Action> FlyCoinRequested;
        public event Action<int,int> CashOutTaskRefreshRequested;
        public RecoveredJackpotMeters JackpotMeters=>jackpotMeters;
        public RecoveredWildPresenter Wilds=>wilds;
        public RecoveredWildSequence WildSequence {get;private set;}
        public event Action<int> JackpotCheckRequested;
        [SerializeField] private RecoveredBonusCollection bonusCollection;
        public RecoveredBonusCollection BonusCollection => bonusCollection;
        [SerializeField] private RecoveredCoinStopPresenter coinStops;
        public RecoveredCoinStopPresenter CoinStops => coinStops;
        [SerializeField] private RecoveredScatterPresenter scatters;
        public RecoveredScatterPresenter Scatters=>scatters;
        [SerializeField] private RecoveredDownWinText downWin;
        public RecoveredDownWinText DownWin=>downWin;
        [SerializeField] private RecoveredDownWinFlight winFlight;
        public RecoveredDownWinFlight WinFlight=>winFlight;
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
            RecoveredPlayerProgress progress, RecoveredGameplayRules gameplayRules, bool isA, int languageType = 0, IAdFacade adFacade = null)
        {
            Unbind(); entry = spinEntry; result = spinResult; rules = gameplayRules;
            if(composition!=null)composition.Bind(GetComponentInParent<Canvas>());
            player=progress;ads=adFacade??new LocalAdFacade();this.isA=isA;language=languageType;
            SymbolWin=new RecoveredSymbolWinSelection(rules);
            symbolAmount.Bind(languageType);
            ordinaryWin.Failed+=WildFailed;
            bigWinSequence.Failed+=WildFailed;
            bigWinPopup.GetComponent<Canvas>().worldCamera=GetComponentInParent<Canvas>().worldCamera;
            bigWinPopup.PauseMusicRequested+=PauseMusic;bigWinPopup.StopSound1Requested+=StopSound1;
            bigWinPopup.ResumeMusicRequested+=ResumeMusic;bigWinPopup.Sound1Requested+=Sound1;bigWinPopup.SoundRequested+=Sound;
            bigWinPopup.FlyCoinRequested+=BigWinFlyCoin;bigWinPopup.CashOutTaskRefreshRequested+=CashOutRefresh;
            jackpotPopup.GetComponent<Canvas>().worldCamera=GetComponentInParent<Canvas>().worldCamera;
            JackpotSequence=new RecoveredJackpotSequence(new RecoveredRewardBranches(rules,progress),jackpotDelay);
            JackpotSequence.WinRequested+=PlayJackpotWin;JackpotSequence.PauseMusicRequested+=PauseMusic;
            JackpotSequence.StopSound1Requested+=StopSound1;JackpotSequence.Sound1Requested+=Sound1;JackpotSequence.Failed+=WildFailed;
            jackpotPopup.PauseMusicRequested+=PauseMusic;jackpotPopup.StopSound1Requested+=StopSound1;
            jackpotPopup.ResumeMusicRequested+=ResumeMusic;jackpotPopup.Sound1Requested+=Sound1;jackpotPopup.SoundRequested+=Sound;
            jackpotPopup.HideWheelRequested+=HideWheel;jackpotPopup.FlyCoinRequested+=FlyCoin;jackpotPopup.CashOutTaskRefreshRequested+=CashOutRefresh;
            BonusCoins = new RecoveredBonusCoinSequence(rules, progress, bonusCoinInterval);
            WildSequence=new RecoveredWildSequence(wildColumnInterval);WildSequence.Failed+=WildFailed;
            if (bonusCollection != null) bonusCollection.Initialize(progress.BonusArea);
            rules.GetBet(isA, progress.Level, bets);
            // GameData.Init resets Bet=0; SetBet only assigns list[0] for the normal branch.
            Bet = isA ? 0 : bets[0];
            if(jackpotMeters!=null) {
                jackpotMeters.Initialize(progress,rules,()=>Bet,languageType);
                entry.JackpotAnimationsRequested+=jackpotMeters.PlayRewardAnim;
            }
            reels.Initialize(symbols);
            wilds.Bind(reels,GetComponentInParent<Canvas>().sortingOrder);
            symbolEffects.Bind(reels,GetComponentInParent<Canvas>().sortingOrder);
            if(scatters!=null){scatters.Bind(reels,GetComponentInParent<Canvas>().sortingOrder);scatters.StopSoundRequested+=ScatterShowSound;}
            if(coinStops!=null)coinStops.Bind(reels,languageType,bonusCollection,GetComponentInParent<Canvas>().sortingOrder,scatters);
            downWin.Bind(languageType);
            winFlight.Bind(GetComponentInParent<Canvas>().sortingOrder);
            coinStops.WinFlightRequested+=winFlight.Play;
            coinStops.RewardRegistered+=downWin.Register;
            coinStops.RewardPresentationFinished+=downWin.PresentationFinished;
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
        [SerializeField] private RecoveredSpinHint spinHint;
        public RecoveredSpinHint SpinHint=>spinHint;
        private void Started() { IsBusy = true; AwaitingRewards = false; Error = null; downWin.Started(); spinButton.PlayAcceptedClick(); spinHint.Hide(); }
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
            downWin.BeginScan();
            RewardSequenceRequested?.Invoke();
            if (entry != null)
                BonusCoins.Begin(result.Board.GetSymbol, PresentBonusCoin,
                    BeginWilds);
        }
        private void WildFailed(Exception error)=>Error=error;
        private void BeginWilds()
        {
            WildColumnsCheckRequested?.Invoke();
            if(entry!=null)WildSequence.Begin(result.Board.GetSymbol,wilds.Present,BeginJackpot);
        }
        private void BeginJackpot(int count)
        {
            JackpotCheckRequested?.Invoke(count);
            if(entry!=null)JackpotSequence.Begin(count,ShowJackpot,BeginSymbolAnimations);
        }
        private void BeginSymbolAnimations()
        {
            SymbolWin.Capture(result.Settlement,result.Board.GetSymbol,BonusCoins.TotalReward,Bet);
            if(SymbolWin.RequestsLineSound)Sound("win");
            if(entry==null)return;
            symbolEffects.Present(SymbolWin,wilds.NextSortingOrder);
            SymbolAnimationsRequested?.Invoke();
            if(entry!=null) {
                symbolAmount.SetSortingOrder(Math.Max(wilds.NextSortingOrder,symbolEffects.NextSortingOrder));
                symbolAmount.Begin(SymbolWin.LineWin,SymbolWin.TotalWin,ReadBonusAmount,AmountReady);
            }
        }
        private float ReadBonusAmount()=>BonusCoins.TotalReward;
        private void AmountReady()
        {
            SymbolAmountReady?.Invoke();if(entry==null)return;
            if(!SymbolWin.HasReward){SymbolSequenceCompleted?.Invoke();return;}
            if(SymbolWin.BigWin!=RecoveredSlotWinType.None) {
                bigWinSequence.Begin(SymbolWin.BigWin,SymbolWin.LineWin,SymbolWin.TotalWin,ReadBonusAmount,
                    player,rules,ads,isA,language,SymbolsCompleted);
                if(entry!=null)BigWinBranchEntered?.Invoke();return;
            }
            ordinaryWin.Begin(SymbolWin.TotalWin,ReadBonusAmount,player,SymbolsCompleted);
        }
        private void SymbolsCompleted()=>SymbolSequenceCompleted?.Invoke();
        public void PlayJackpotWin(RecoveredJackpotType type)
        {
            // JackPotAnim 23bc60c selects Grand/Major/otherwise Mini. Its callback
            // 23bf84c resets the persisted counter before that meter refreshes.
            int index=type==RecoveredJackpotType.Grand?0:type==RecoveredJackpotType.Major?1:2;
            jackpotMeters.At(index).PlayAnim(()=>player.SetJpAddCount(0));
        }
        private void ShowJackpot(RecoveredJackpotType type,float amount,Action<float> completed)
            =>jackpotPopup.Show(type,amount,player,rules,ads,isA,language,completed);
        private void PauseMusic()=>PauseMusicRequested?.Invoke();
        private void StopSound1()=>StopSound1Requested?.Invoke();
        private void ResumeMusic()=>ResumeMusicRequested?.Invoke();
        private void Sound(string name)=>SoundRequested?.Invoke(name);
        private void ScatterShowSound()=>Sound("scatterShow");
        private void Sound1(string name)=>Sound1Requested?.Invoke(name);
        private void HideWheel()=>HideWheelRequested?.Invoke();
        private void FlyCoin(float amount,Action completed)=>FlyCoinRequested?.Invoke(amount,completed);
        private void BigWinFlyCoin(float amount)=>FlyCoin(amount,null);
        private void CashOutRefresh(int task,int amount)=>CashOutTaskRefreshRequested?.Invoke(task,amount);
        private void PresentBonusCoin(RecoveredBonusCoin coin)
        {
            if (coinStops != null) coinStops.PlayRewardReveal(coin.Column, coin.Row, coin.Reward, coin.CollectionCount);
            BonusCoinPresentationRequested?.Invoke(coin);
        }
        // Called by the eventual CheckBaseEnd completion, never by a reel stop callback.
        public void CompleteBaseRound()
        {
            if (!AwaitingRewards) throw new InvalidOperationException("The reel sequence has not reached rewards.");
            AwaitingRewards = false; IsBusy = false;
        }
        public void Unbind()
        {
            if(bigWinSequence!=null){bigWinSequence.Failed-=WildFailed;bigWinSequence.Cancel();}
            if(bigWinPopup!=null) {
                bigWinPopup.PauseMusicRequested-=PauseMusic;bigWinPopup.StopSound1Requested-=StopSound1;
                bigWinPopup.ResumeMusicRequested-=ResumeMusic;bigWinPopup.Sound1Requested-=Sound1;bigWinPopup.SoundRequested-=Sound;
                bigWinPopup.FlyCoinRequested-=BigWinFlyCoin;bigWinPopup.CashOutTaskRefreshRequested-=CashOutRefresh;
            }
            JackpotSequence?.Cancel();JackpotSequence=null;
            if(symbolEffects!=null)symbolEffects.Unbind();
            if(ordinaryWin!=null){ordinaryWin.Failed-=WildFailed;ordinaryWin.Cancel();}
            if(symbolAmount!=null)symbolAmount.Cancel();
            SymbolWin=null;
            if(jackpotPopup!=null) {
                jackpotPopup.PauseMusicRequested-=PauseMusic;jackpotPopup.StopSound1Requested-=StopSound1;
                jackpotPopup.ResumeMusicRequested-=ResumeMusic;jackpotPopup.Sound1Requested-=Sound1;jackpotPopup.SoundRequested-=Sound;
                jackpotPopup.HideWheelRequested-=HideWheel;jackpotPopup.FlyCoinRequested-=FlyCoin;jackpotPopup.CashOutTaskRefreshRequested-=CashOutRefresh;
                jackpotPopup.gameObject.SetActive(false);
            }
            rewardWait?.Cancel(); rewardWait = null;
            BonusCoins?.CancelForProfileChange(); BonusCoins = null;
            WildSequence?.Cancel();WildSequence=null;if(wilds!=null)wilds.Unbind();
            if(winFlight!=null) {winFlight.Cancel();if(coinStops!=null)coinStops.WinFlightRequested-=winFlight.Play;}
            if(downWin!=null) { downWin.Cancel(); if(coinStops!=null) {coinStops.RewardRegistered-=downWin.Register;coinStops.RewardPresentationFinished-=downWin.PresentationFinished;} }
            if(coinStops!=null)coinStops.Unbind();
            if(scatters!=null){scatters.StopSoundRequested-=ScatterShowSound;scatters.Unbind();}
            spinButton.Button.onClick.RemoveListener(Click);
            if (entry != null) {
                entry.StartVisualsRequested -= Started;
                if(jackpotMeters!=null)entry.JackpotAnimationsRequested-=jackpotMeters.PlayRewardAnim;
            }
            if(jackpotMeters!=null)jackpotMeters.Cancel();
            reels.ReelsStopped -= Stopped;
            reels.AbortForProfileChange();
            entry = null; result = null; rules = null; IsBusy = false; AwaitingRewards = false; Error = null;
            player=null;ads=null;
        }
        private void OnDestroy() => Unbind();
    }
}
