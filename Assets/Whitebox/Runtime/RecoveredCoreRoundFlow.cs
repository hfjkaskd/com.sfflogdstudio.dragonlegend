using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Core continuation after Base symbol/Bonus settlement and through Free rounds.
    public sealed class RecoveredCoreRoundFlow : MonoBehaviour
    {
        [SerializeField] private RecoveredFreeEntryFlow entry;
        [SerializeField] private RecoveredFreeExitFlow exit;
        [SerializeField] private RecoveredFreeSlotGame slot;
        [SerializeField] private RecoveredFreeWheelGame wheel;
        [SerializeField] private RecoveredFreeTreasureGame treasure;
        [SerializeField] private RecoveredFreeLuckyGame lucky;
        [SerializeField] private RectTransform ballDestination;
        [SerializeField] private RecoveredMoreSpinWindow moreSpins;
        [SerializeField] private RecoveredMoreWildWindow moreWild;
        [SerializeField] private RecoveredBankWindow bank;
        public RecoveredBankWindow Bank=>bank;
        private bool bankPending;
        private RecoveredReelWait bankWait;
        [SerializeField] private RecoveredReviewWindow reviewPrefab;
        private RecoveredReviewWindow review;
        private bool reviewPending;
        private RecoveredReelWait reviewWait;
        public RecoveredReviewWindow Review=>review;
        [SerializeField] private RecoveredCashPromptWindow cashPromptPrefab;
        [SerializeField] private RecoveredCashOutWindow cashOutPrefab;
        [SerializeField] private int popupBaseDepth;
        private readonly List<Canvas> popupCanvases=new List<Canvas>(16);
        private RecoveredCashPromptWindow cashPrompt;
        private RecoveredCashOutWindow cashOut;
        private bool cashPromptPending;
        private RecoveredReelWait cashPromptWait;
        public RecoveredCashPromptWindow CashPrompt=>cashPrompt;
        public bool IsCashPromptPending=>cashPromptPending;
        public RecoveredCashOutWindow CashOut=>cashOut;
        public event Action<string> SoundRequested;
        public RecoveredMoreWildWindow MoreWild=>moreWild;
        [SerializeField] private RecoveredTipsWindow tips;
        [SerializeField] private RecoveredFirstSpinGuide firstSpinGuide;
        public RecoveredFirstSpinGuide FirstSpinGuide=>firstSpinGuide;
        [SerializeField] private string moreSpinLimitMessage;
        public RecoveredMoreSpinWindow MoreSpins=>moreSpins;
        public RecoveredTipsWindow Tips=>tips;
        private GameEntry game;
        private RecoveredFreeSmallGameRouter router;
        public RecoveredFreeEntryFlow Entry=>entry;
        public RecoveredFreeExitFlow Exit=>exit;
        public RecoveredFreeSmallGameRouter Router=>router;
        public Exception Error {get;private set;}
        public event Action CoreRoundCompleted;
        public void Bind(GameEntry context)
        {
            game=context;var field=game.Playfield;var reels=field.ModeView.FreeReels;
            field.ModeView.HideSpeedEffects();
            field.Reels.AnticipationVisibilityRequested+=field.ModeView.SetSpeedEffect;
            field.Reels.ShakeRequested+=field.Wilds.Shake.Begin;
            reels.Controller.ShakeRequested+=field.Wilds.Shake.Begin;
            moreSpins.Bind(game);tips.Bind(game.GetComponent<Canvas>().worldCamera);
            moreWild.Bind(game);
            bankPending=false;
            reviewPending=false;game.PlayerProgress.ReviewRequested+=ReviewReady;
            bank.Bind(game.PlayerProgress,game.Rules,game.Ads,()=>game.Playfield.Bet,game.CurrentProfile.languageType,game.GetComponent<Canvas>().worldCamera);
            bank.FlyRequested+=BankFly;game.PlayerProgress.BankReady+=BankReady;
            field.BankProgress.Bind(game.PlayerProgress,game.Rules,tips.Show);
            field.MoreWildEntry.Bind(game,ShowMoreWild);
            field.SpinRecovery.Bind(game,moreSpins.Show);
            game.SpinEntry.MoreSpinsRequested+=moreSpins.Show;moreSpins.LimitTipRequested+=ShowMoreSpinLimit;
            var player=game.PlayerProgress;var rules=game.Rules;var profile=game.CurrentProfile;
            var symbols=new int[field.Symbols.ModeCount(RecoveredSlotType.Free)];
            for(int i=0;i<symbols.Length;i++)symbols[i]=field.Symbols.ModeId(RecoveredSlotType.Free,i);
            entry.Bind(field,player,rules,game.RewardBranches,game.FreeSpinResult,game.FreeSpinEntry,symbols,game.Ads);
            entry.InitShowRequested+=field.ModeView.ApplyCurrent;
            entry.InitFreeReelsRequested+=field.ModeView.InitializeFreeReels;
            entry.InitFreeSpinTimesRequested+=InitialCount;
            entry.Completed+=AfterEntry;entry.Failed+=Fail;
            slot.Bind(player,rules,game.Ads,game.CashFlight,game.transform,profile.isA,profile.languageType);
            wheel.Bind(player,rules,game.Ads,game.CashFlight,game.transform,field,profile.isA,profile.languageType);
            field.CashOutTaskRefreshRequested+=player.RefreshCashOutTask;
            game.BonusFlow.Window.CashOutTaskRefreshRequested+=player.RefreshCashOutTask;
            wheel.Window.CashOutTaskRefreshRequested+=player.RefreshCashOutTask;
            entry.CashOutTaskRefreshRequested+=player.RefreshCashOutTask;
            treasure.Bind(player,rules,game.Ads,game.CashFlight,game.transform,profile.isA,profile.languageType,game.CollectEntry.Destination);
            lucky.Bind(player,rules,game.Ads,game.CashFlight,game.transform,profile.isA,profile.languageType);
            router=new RecoveredFreeSmallGameRouter(rules,player,slot.Begin,wheel.Begin,treasure.Begin,lucky.Begin);
            reels.CoinScan.Bind(rules,player,game.FreeSpinResult,field.BonusCollection,Language);
            game.BonusFlow.BindFreeScan(reels.CoinScan);
            reels.BallScan.Bind(game.FreeSpinResult,player,field.Npc,ballDestination,field.Npc.transform.Find("PlayFire"),Language,router.Open,game.BonusFlow);
            reels.RewardCollect.Bind(game.FreeSpinResult,player,field.DownWin,(RectTransform)field.FreeBottom.transform,game.GetComponent<Canvas>().sortingOrder,entry);
            exit.Bind(reels,player,game.FreeSpinResult,game.FreeSpinEntry,symbols,()=>entry.InitialSpinCount,game.transform,Language);
            exit.BaseViewResetRequested+=field.ModeView.ApplyCurrent;
            exit.BaseReelsInitRequested+=InitializeBase;
            exit.FreeEndFlagClearRequested+=ReturnedToBase;
            game.BonusFlow.Completed+=AfterBonus;
            field.SpinHint.Begin();
            game.SpinEntry.GuideHideRequested+=firstSpinGuide.Hide;
            if(game.PlayerStore.Data.GuideStep==1)
            {
                firstSpinGuide.Show((RectTransform)game.transform,(RectTransform)field.SpinButton.transform,game.GetComponent<Canvas>().worldCamera);
                field.SpinHint.ShowImmediate();
            }
        }
        private int Language()=>game.CurrentProfile.languageType;
        private void ShowMoreWild(){PreparePopupDepth(moreWild);moreWild.Show(false);}
        private void ShowMoreSpinLimit()=>tips.Show(moreSpinLimitMessage);
        private void InitialCount(int count)=>game.Playfield.ModeView.RefreshFreeCount();
        private void InitializeBase()=>game.Playfield.Reels.Initialize(game.Playfield.Symbols);
        private void AfterBonus()
        {
            if(game.PlayerProgress.GameSlotType!=RecoveredSlotType.Base||!game.Playfield.AwaitingRewards)return;
            entry.Begin(game.SpinResult.ScatterCount);
        }
        private void AfterEntry(){if(!entry.IsFreeSpinEnd)CompleteCoreRound();}
        private void ReturnedToBase(){entry.ClearFreeEndFlag();CompleteCoreRound();}
        private void CompleteCoreRound()
        {
            // Main.BankPop 23bc778 only sets a pending flag; CheckBaseEnd waits after Free returns.
            if(bankPending)
            {
                bank.Show(()=>{bankPending=false;game.Playfield.BankProgress.Refresh();});
            }
            // Native waits are outside the conditional ShowWindow branches.
            bankWait=RecoveredReelWait.Until(()=>!bankPending,()=>{bankWait=null;AfterBank();},Fail);
        }
        private void ReviewReady()=>reviewPending=true;
        private void AfterBank()
        {
            if(reviewPending)
            {
                if(review==null)
                {
                    review=Instantiate(reviewPrefab,transform,false);
                    review.SoundRequested+=Sound;
                }
                review.Bind(game.GetComponent<Canvas>().worldCamera,Application.identifier,Application.OpenURL);
                review.Show(()=>reviewPending=false);
            }
            reviewWait=RecoveredReelWait.Until(()=>!reviewPending,()=>{reviewWait=null;AfterReview();},Fail);
        }
        private void AfterReview()
        {
            // CheckBaseEnd 23c6068..23c6500: first unrecorded tier, then an unconditional wait.
            cashPromptPending=false;
            int tier=game.PlayerProgress.PrepareCashOutPrompt();
            if(tier>=0)
            {
                cashPromptPending=true;
                if(cashPrompt==null)
                {
                    cashPrompt=Instantiate(cashPromptPrefab,transform,false);
                    cashPrompt.Bind(game.GetComponent<Canvas>().worldCamera,OpenCashOut);
                    cashPrompt.SoundRequested+=Sound;
                }
                PreparePopupDepth(cashPrompt);
                cashPrompt.Show(tier,game.Rules,Language(),()=>cashPromptPending=false);
            }
            cashPromptWait=RecoveredReelWait.Until(()=>!cashPromptPending,()=>{cashPromptWait=null;FinishCoreRound();},Fail);
        }
        private void OpenCashOut()
        {
            if(game==null)return;
            if(cashOut==null)
            {
                cashOut=Instantiate(cashOutPrefab,transform,false);
                cashOut.Bind(game.Rules,game.PlayerProgress,Language(),game.GetComponent<Canvas>(),UtcNow);
                cashOut.SoundRequested+=Sound;
            }
            PreparePopupDepth(cashOut);
            cashOut.Show();
        }
        private void PreparePopupDepth(Component window)
        {
            if(window.gameObject.activeSelf)return;
            // BaseUIManager.AdjustWindowDepth 33be5f4: max(baseDepth, active max + 1).
            // These three prefabs each have one Canvas, with no independent child UIOrder.
            GetComponentsInChildren(false,popupCanvases);int depth=popupBaseDepth;
            foreach(var canvas in popupCanvases)depth=Math.Max(depth,canvas.sortingOrder+1);
            popupCanvases.Clear();window.GetComponent<Canvas>().sortingOrder=depth;
            window.transform.SetAsLastSibling();
        }
        private static int UtcNow()=>unchecked((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        private void Sound(string name)=>SoundRequested?.Invoke(name);
        private void BankReady()=>bankPending=true;
        private void BankFly(float amount,Action completed,Transform source)=>game.CashFlight.Begin(amount,completed,bank.transform,false,source);
        private void FinishCoreRound()
        {
            // CheckBaseEnd 23c673c..23c67c0: show free Wild before unlocking, without awaiting claim.
            if(game.PlayerStore.Data.GuideStep==2){PreparePopupDepth(moreWild);moreWild.Show(true);}
            game.Playfield.SpinHint.Begin();
            if(game.Playfield.AwaitingRewards)game.Playfield.CompleteBaseRound();
            // CheckBaseEnd 23c67d4..23c67f4: clear the busy flag, then SavePlayerData.
            game.PlayerStore.Save();
            CoreRoundCompleted?.Invoke();
        }
        private void Fail(Exception error)=>Error=error;
        public void Unbind()
        {
            if(game==null)return;
            game.Playfield.Reels.ShakeRequested-=game.Playfield.Wilds.Shake.Begin;
            game.Playfield.ModeView.FreeReels.Controller.ShakeRequested-=game.Playfield.Wilds.Shake.Begin;
            game.Playfield.Wilds.Shake.Cancel();
            game.Playfield.Reels.AnticipationVisibilityRequested-=game.Playfield.ModeView.SetSpeedEffect;
            game.Playfield.ModeView.HideSpeedEffects();
            game.Playfield.CashOutTaskRefreshRequested-=game.PlayerProgress.RefreshCashOutTask;
            game.BonusFlow.Window.CashOutTaskRefreshRequested-=game.PlayerProgress.RefreshCashOutTask;
            wheel.Window.CashOutTaskRefreshRequested-=game.PlayerProgress.RefreshCashOutTask;
            entry.CashOutTaskRefreshRequested-=game.PlayerProgress.RefreshCashOutTask;
            game.SpinEntry.GuideHideRequested-=firstSpinGuide.Hide;firstSpinGuide.Hide();
            game.SpinEntry.MoreSpinsRequested-=moreSpins.Show;moreSpins.LimitTipRequested-=ShowMoreSpinLimit;
            moreSpins.Cancel();tips.Cancel();
            moreWild.Cancel();
            game.PlayerProgress.BankReady-=BankReady;bank.FlyRequested-=BankFly;
            bankWait?.Cancel();bankWait=null;bank.Cancel();bankPending=false;
            game.Playfield.BankProgress.Unbind();
            game.PlayerProgress.ReviewRequested-=ReviewReady;reviewWait?.Cancel();reviewWait=null;
            if(review!=null){review.Cancel();review.SoundRequested-=Sound;}reviewPending=false;
            cashPromptWait?.Cancel();cashPromptWait=null;cashPromptPending=false;
            if(cashPrompt!=null){cashPrompt.Cancel();cashPrompt.SoundRequested-=Sound;}
            if(cashOut!=null){cashOut.Unbind();cashOut.SoundRequested-=Sound;}
            game.Playfield.MoreWildEntry.Unbind();
            game.Playfield.SpinRecovery.Unbind();
            game.BonusFlow.Completed-=AfterBonus;game.BonusFlow.BindFreeScan(null);
            var mode=game.Playfield.ModeView;
            game.Playfield.SpinHint.Hide();
            mode.FreeReels.Controller.AbortForProfileChange();
            entry.InitShowRequested-=mode.ApplyCurrent;entry.InitFreeReelsRequested-=mode.InitializeFreeReels;
            entry.InitFreeSpinTimesRequested-=InitialCount;entry.Completed-=AfterEntry;entry.Failed-=Fail;entry.Unbind();
            exit.BaseViewResetRequested-=mode.ApplyCurrent;exit.BaseReelsInitRequested-=InitializeBase;
            exit.FreeEndFlagClearRequested-=ReturnedToBase;exit.Unbind();game=null;
        }
        private void OnDestroy()=>Unbind();
    }
}
