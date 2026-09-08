using System;

namespace DragonLegend.Whitebox
{
    // UIJackpotView presentation boundary. Implementations bind authored prefab objects;
    // count completion and AfterHide must come from their actual animations.
    public interface IRecoveredJackpotClaimView
    {
        void PlaySound(string name);
        void CountReward(float from, float to, Action completed);
        void HideWheelIfShown();
        void HideJackpot();
        void StopSound1();
        void ResumeMusic();
        void FlyCoin(float amount, Action completed);
    }

    // UIJackpotView 0x23b2b2c / 0x23b32a8 / 0x23b3680.
    // No balance mutation here: PlayFlyCoin owns credit AFTER the caller callback.
    public sealed class RecoveredJackpotClaim
    {
        private readonly RecoveredPlayerProgress progress;
        private readonly RecoveredGameplayRules rules;
        private readonly IAdFacade ads;
        private readonly IRecoveredJackpotClaimView view;
        private Action<float> callback;
        public float OriginalReward { get; private set; }
        public float Reward { get; private set; }
        public float AdvertisedMultiplier { get; private set; }
        public float UnadvertisedMultiplier { get; private set; }
        public bool FirstFreeAtShow { get; private set; }
        public bool IsClicked { get; private set; }

        public RecoveredJackpotClaim(RecoveredPlayerProgress progress,
            RecoveredGameplayRules rules, IAdFacade ads, IRecoveredJackpotClaimView view)
        {
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            this.ads = ads ?? throw new ArgumentNullException(nameof(ads));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void BeforeShow(float reward, Action<float> completed)
        {
            IsClicked = false;
            Reward = OriginalReward = reward;
            callback = completed;
            FirstFreeAtShow = progress.IsFirstFreeReward;
            AdvertisedMultiplier = FirstFreeAtShow ? 1 : rules.GetJpClaim(0);
            UnadvertisedMultiplier = rules.GetJpClaim(1);
        }

        // Call after BaseWindow.OnClickButton. Original sets the latch even for an
        // unknown button name. There is deliberately no synthetic close action.
        public void OnClickButton(string name)
        {
            if (IsClicked) return;
            IsClicked = true;
            if (name == "ClaimBtn")
            {
                view.PlaySound("click");
                if (progress.IsFirstFreeReward) RewardAdSucceeded();
                else ads.PlayRewardAd(RewardAdSucceeded, RewardAdFailed, "jackpot", "jackpot");
            }
            else if (name == "UnPlayBtn")
            {
                view.PlaySound("click");
                ads.PlayInterAd("iv_close", "jackpot");
                Reward *= UnadvertisedMultiplier;
                FinishCall();
            }
        }

        private void RewardAdSucceeded()
        {
            Reward *= AdvertisedMultiplier;
            FinishCall();
        }

        private void RewardAdFailed() => IsClicked = false;

        private void FinishCall()
        {
            if (OriginalReward == Reward) FinishAction();
            else
            {
                view.PlaySound("count");
                // Original curCount getter remains unchanged throughout the tween.
                view.CountReward(OriginalReward, Reward, FinishAction);
            }
        }

        private void FinishAction()
        {
            view.HideWheelIfShown();
            view.HideJackpot();
        }

        // Invoked by the window AFTER its exit animation and deactivation.
        public void AfterHide()
        {
            view.StopSound1();
            view.ResumeMusic();
            view.FlyCoin(Reward, FlyCoinCompleted);
        }

        private void FlyCoinCompleted() => callback?.Invoke(Reward);
    }
}
