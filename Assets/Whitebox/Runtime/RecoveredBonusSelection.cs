using System;

namespace DragonLegend.Whitebox
{
    public interface IRecoveredBonusSelectionView
    {
        void HideFinger();
        void CancelFingerSequence();
        void InitializeCards();
        void SetCloseVisible(bool visible);
        void SetCardEnabled(int index,bool enabled);
        void ShowCardAd(int index,bool visible);
        void SetChances(int selected,int free);
        void PlayCard(int index,RecoveredBonusRound.Reveal reveal,Action releaseInput);
        void ShowFinger();
        void HideBonus();
    }

    // UIBonusView selection path. The authored window supplies cards and their
    // real animation/popup release callbacks; it owns exit/source completion.
    public sealed class RecoveredBonusSelection
    {
        private readonly RecoveredGameplayRules rules;
        private readonly IAdFacade ads;
        private readonly IRecoveredBonusSelectionView view;
        private readonly int visibleCardCount;
        public RecoveredBonusRound Round {get;}=new RecoveredBonusRound();
        public bool IsClicked {get;private set;}
        public bool NeedsAd {get;private set;}
        public bool IsEnd {get;private set;}
        public RecoveredBonusSelection(RecoveredGameplayRules rules,IAdFacade ads,IRecoveredBonusSelectionView view,int visibleCardCount)
        {
            this.rules=rules??throw new ArgumentNullException(nameof(rules));
            this.ads=ads??throw new ArgumentNullException(nameof(ads));
            this.view=view??throw new ArgumentNullException(nameof(view));
            this.visibleCardCount=visibleCardCount;
        }
        public void BeforeShow()
        {
            view.CancelFingerSequence();view.HideFinger();IsEnd=false;
            view.SetCloseVisible(false);Round.Initialize(rules);view.InitializeCards();RefreshChances();
            // Native does not clear isClick or isNeedPlayAd here (23997b4).
        }
        public void AfterShow()=>view.ShowFinger();
        public bool RequestClose()
        {
            if(IsEnd)return false;
            IsEnd=true;return true;
        }
        public void Select(int index)
        {
            if(IsClicked)return;
            view.HideFinger();view.CancelFingerSequence();IsClicked=true;
            view.SetCardEnabled(index,false);
            if(Round.WasClicked(index))return;
            if(!NeedsAd){Accept(index);return;}
            ads.PlayRewardAd(()=>{view.ShowCardAd(index,false);Accept(index);},()=>{
                // Exact source behavior: restore the Button and hint, but retain
                // isClick. No synthetic retry-unlock absent from 239c3f4.
                view.SetCardEnabled(index,true);view.ShowFinger();
            },"bonusCoin","bonus");
        }
        private void Accept(int index)
        {
            if(!Round.TryReveal(index,out var reveal))return;
            RefreshChances();view.PlayCard(index,reveal,ReleaseInput);
        }
        private void RefreshChances()
        {
            int free=rules.GetBonusFreeTimes();
            if(Round.ClickedCount==free){
                view.SetCloseVisible(true);NeedsAd=true;
                for(int i=0;i<visibleCardCount;i++)if(!Round.WasClicked(i))view.ShowCardAd(i,true);
            }
            view.SetChances(Round.ClickedCount,free);
        }
        private void ReleaseInput()
        {
            if(Round.ClickedCount==visibleCardCount){IsEnd=true;view.HideBonus();}
            IsClicked=false;view.ShowFinger();
        }
    }
}
