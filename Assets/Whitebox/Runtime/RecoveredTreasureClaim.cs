using System;

namespace DragonLegend.Whitebox
{
    public interface IRecoveredTreasureClaimView
    {
        void PlaySound(string name);
        void CountReward(Func<float> readOriginal,float to,Action completed);
        void HideTreasure();
        void FlyCoin(float amount,Action completed);
        void FlyCollectCard();
    }

    // UITreasureView 23dc94c / 23dd198 / 23dd460 / 23dcca0.
    public sealed class RecoveredTreasureClaim
    {
        private readonly IAdFacade ads;
        private readonly IRecoveredTreasureClaimView view;
        private RecoveredCollectInfo info;
        private Action<float> callback;
        public float AdvertisedMultiplier {get;private set;}
        public float UnadvertisedMultiplier {get;private set;}
        public float Reward {get;private set;}
        public bool IsClicked {get;private set;}

        public RecoveredTreasureClaim(IAdFacade ads,IRecoveredTreasureClaimView view)
        {
            this.ads=ads??throw new ArgumentNullException(nameof(ads));
            this.view=view??throw new ArgumentNullException(nameof(view));
        }
        public void BeforeShow(RecoveredCollectInfo card,RecoveredGameplayRules rules,Action<float> completed)
        {
            IsClicked=false;info=card;callback=completed;
            AdvertisedMultiplier=rules.GetCollectClaim(0);
            UnadvertisedMultiplier=rules.GetCollectClaim(1);
            // Native OnBeforeShow never writes reward (+0xbc); retain the previous value.
        }
        public void OnClickButton(string name)
        {
            if(IsClicked)return;
            IsClicked=true;
            if(name=="ClaimBtn") {
                view.PlaySound("click");
                ads.PlayRewardAd(RewardAdSucceeded,()=>IsClicked=false,"treasure","treasure");
            } else if(name=="UnPlayBtn") {
                view.PlaySound("click");ads.PlayInterAd("iv_close","treasure");
                Reward=(float)info.worth*UnadvertisedMultiplier;
                FinishCall();
            }
        }
        private void RewardAdSucceeded()
        {
            Reward=(float)info.worth*AdvertisedMultiplier;
            FinishCall();
        }
        private float ReadOriginalReward() => info.worth;
        private void FinishCall()
        {
            if(Reward==(float)info.worth)view.HideTreasure();
            else {
                view.PlaySound("count");
                view.CountReward(ReadOriginalReward,Reward,view.HideTreasure);
            }
        }
        public void AfterHide()
        {
            // Events "1" then "14". The first callback reads current reward on arrival.
            view.FlyCoin(Reward,()=>callback?.Invoke(Reward));
            view.FlyCollectCard();
        }
    }
}
