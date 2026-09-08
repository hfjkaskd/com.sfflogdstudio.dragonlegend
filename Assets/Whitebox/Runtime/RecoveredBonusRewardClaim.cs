using System;

namespace DragonLegend.Whitebox
{
    public interface IRecoveredBonusRewardClaimView
    {
        void PlaySound(string name);
        void CountReward(float from,float to,Action completed);
        void HideWheelIfShown();
        void HideReward();
        void FlyCoin(float amount,Action completed);
    }

    // UIRewardView (23d8174, 23d8470, 23d89e4, 23d87e0).
    // The authored presenter supplies the native 2 / .5 multipliers and the
    // .5-second count animation; the shared cash flight owns balance changes.
    public sealed class RecoveredBonusRewardClaim
    {
        private readonly IAdFacade ads;
        private readonly IRecoveredBonusRewardClaimView view;
        public float AdvertisedMultiplier {get;}
        public float UnadvertisedMultiplier {get;}
        public float OriginalReward {get;private set;}
        public float Reward {get;private set;}
        public bool IsClicked {get;private set;}
        private Action<float> callback;

        public RecoveredBonusRewardClaim(IAdFacade ads,IRecoveredBonusRewardClaimView view,
            float advertisedMultiplier,float unadvertisedMultiplier)
        {
            this.ads=ads??throw new ArgumentNullException(nameof(ads));
            this.view=view??throw new ArgumentNullException(nameof(view));
            AdvertisedMultiplier=advertisedMultiplier;UnadvertisedMultiplier=unadvertisedMultiplier;
        }
        public void BeforeShow(float reward,Action<float> completed)
        {
            IsClicked=false;OriginalReward=Reward=reward;callback=completed;
        }
        public void OnClickButton(string name)
        {
            if(IsClicked)return;
            IsClicked=true;
            if(name=="ClaimBtn"){
                view.PlaySound("click");
                ads.PlayRewardAd(RewardAdSucceeded,()=>IsClicked=false,"lucky","lucky");
            }else if(name=="UnPlayBtn"){
                view.PlaySound("click");ads.PlayInterAd("iv_close","lucky");
                Reward*=UnadvertisedMultiplier;FinishCall();
            }
        }
        private void RewardAdSucceeded(){Reward*=AdvertisedMultiplier;FinishCall();}
        private void FinishCall()
        {
            if(OriginalReward==Reward)FinishAction();
            else {view.PlaySound("count");view.CountReward(OriginalReward,Reward,FinishAction);}
        }
        private void FinishAction(){view.HideWheelIfShown();view.HideReward();}
        // Called only after the real window exit. No StopSound1/ResumeMusic here.
        public void AfterHide()=>view.FlyCoin(Reward,()=>callback?.Invoke(Reward));
    }
}
