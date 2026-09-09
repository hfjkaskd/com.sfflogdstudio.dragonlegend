using System;
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
            moreSpins.Bind(game);tips.Bind(game.GetComponent<Canvas>().worldCamera);
            moreWild.Bind(game);
            bankPending=false;
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
        private void ShowMoreWild()=>moreWild.Show(false);
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
                bankWait=RecoveredReelWait.Until(()=>!bankPending,()=>{bankWait=null;FinishCoreRound();},Fail);
                return;
            }
            FinishCoreRound();
        }
        private void BankReady()=>bankPending=true;
        private void BankFly(float amount,Action completed,Transform source)=>game.CashFlight.Begin(amount,completed,bank.transform,false,source);
        private void FinishCoreRound()
        {
            // CheckBaseEnd 23c673c..23c67c0: show free Wild before unlocking, without awaiting claim.
            if(game.PlayerStore.Data.GuideStep==2)moreWild.Show(true);
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
            game.SpinEntry.GuideHideRequested-=firstSpinGuide.Hide;firstSpinGuide.Hide();
            game.SpinEntry.MoreSpinsRequested-=moreSpins.Show;moreSpins.LimitTipRequested-=ShowMoreSpinLimit;
            moreSpins.Cancel();tips.Cancel();
            moreWild.Cancel();
            game.PlayerProgress.BankReady-=BankReady;bank.FlyRequested-=BankFly;
            bankWait?.Cancel();bankWait=null;bank.Cancel();bankPending=false;
            game.Playfield.BankProgress.Unbind();
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
