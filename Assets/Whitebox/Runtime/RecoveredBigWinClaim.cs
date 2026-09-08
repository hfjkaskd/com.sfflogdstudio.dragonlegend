using System;

namespace DragonLegend.Whitebox
{
    public interface IRecoveredBigWinClaimView
    {
        void PlaySound(string name);
        void CountReward(float from, float to, Action completed);
        void HideBigWin();
        void StopSound1();
        void ResumeMusic();
        void FlyCoin(float amount);
    }

    // UIBigWinView 2394a14 / 239519c / 2395548. The presenter owns the
    // .5-second count, window exit and event-1 cash flight; this class never credits.
    public sealed class RecoveredBigWinClaim
    {
        private readonly RecoveredPlayerProgress progress;
        private readonly RecoveredGameplayRules rules;
        private readonly IAdFacade ads;
        private readonly IRecoveredBigWinClaimView view;
        private Action<float> callback;
        public float OriginalReward { get; private set; }
        public float Reward { get; private set; }
        public float AdvertisedMultiplier { get; private set; }
        public float UnadvertisedMultiplier { get; private set; }
        public bool FirstFreeAtShow { get; private set; }
        public bool IsClicked { get; private set; }

        public RecoveredBigWinClaim(RecoveredPlayerProgress progress, RecoveredGameplayRules rules,
            IAdFacade ads, IRecoveredBigWinClaimView view)
        {
            this.progress=progress??throw new ArgumentNullException(nameof(progress));
            this.rules=rules??throw new ArgumentNullException(nameof(rules));
            this.ads=ads??throw new ArgumentNullException(nameof(ads));
            this.view=view??throw new ArgumentNullException(nameof(view));
        }
        public void BeforeShow(float reward, Action<float> completed)
        {
            IsClicked=false;Reward=0;OriginalReward=reward;callback=completed;
            FirstFreeAtShow=progress.IsFirstFreeReward;
            AdvertisedMultiplier=FirstFreeAtShow?1:rules.GetBigWinClaim(0);
            UnadvertisedMultiplier=rules.GetBigWinClaim(1);
        }
        public void OnClickButton(string name)
        {
            if(IsClicked)return;
            IsClicked=true;
            if(name=="ClaimBtn") {
                view.PlaySound("click");
                // Native checks the live flag on click, but retains the show-time multiplier.
                if(progress.IsFirstFreeReward)RewardAdSucceeded();
                else ads.PlayRewardAd(RewardAdSucceeded,RewardAdFailed,"bigwin","bigwin");
            } else if(name=="UnPlayBtn") {
                view.PlaySound("click");ads.PlayInterAd("iv_close","bigwin");
                Reward=OriginalReward*UnadvertisedMultiplier;FinishCall();
            }
        }
        private void RewardAdSucceeded(){Reward=OriginalReward*AdvertisedMultiplier;FinishCall();}
        private void RewardAdFailed()=>IsClicked=false;
        private void FinishCall()
        {
            if(OriginalReward==Reward)view.HideBigWin();
            else {view.PlaySound("count");view.CountReward(OriginalReward,Reward,view.HideBigWin);}
        }
        public void AfterHide()
        {
            view.StopSound1();view.ResumeMusic();
            callback?.Invoke(Reward);
            // Native dispatches event "1" AFTER the main-flow callback, unlike Jackpot.
            view.FlyCoin(Reward);
        }
    }
}
