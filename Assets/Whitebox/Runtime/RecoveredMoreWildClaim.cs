using System;
using DragonLegend.Whitebox.Recovered;

namespace DragonLegend.Whitebox
{
    public interface IRecoveredMoreWildView
    {
        void PlaySound(string sound);
        void HideFinger();
        void Hide();
    }

    // UIMoreWildView: 23d5c54 / 23d5ee8 / 23d6230 / 23d63a0.
    public sealed class RecoveredMoreWildClaim
    {
        private readonly RecoveredGameplayRules rules;
        private readonly RecoveredPlayerProgress player;
        private readonly PlayerData data;
        private readonly IAdFacade ads;
        private readonly IRecoveredMoreWildView view;
        private bool cancelled;
        public bool IsFree { get; private set; }
        public bool IsClicked { get; private set; }

        public RecoveredMoreWildClaim(RecoveredGameplayRules config, RecoveredPlayerProgress progress,
            PlayerData record, IAdFacade facade, IRecoveredMoreWildView presenter)
        {
            rules = config ?? throw new ArgumentNullException(nameof(config));
            player = progress ?? throw new ArgumentNullException(nameof(progress));
            data = record ?? throw new ArgumentNullException(nameof(record));
            ads = facade ?? throw new ArgumentNullException(nameof(facade));
            view = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        public int BeforeShow(bool isFree)
        {
            cancelled = false;
            IsFree = isFree;
            IsClicked = false;
            return rules.GetMoreWild();
        }

        public void Cancel() => cancelled = true;

        public void Click(string name)
        {
            if (cancelled || IsClicked) return;
            // Native 23d5fec writes zero, unlike UIMoreSpinView's click latch.
            IsClicked = false;
            if (name == "ClaimBtn")
            {
                view.PlaySound("click");
                view.HideFinger();
                if (IsFree)
                {
                    data.GuideStep = unchecked(data.GuideStep + 1);
                    Succeeded();
                }
                else ads.PlayRewardAd(Succeeded, Failed, "morewild", "morewild");
            }
            else if (name == "CloseBtn")
            {
                view.PlaySound("click");
                view.Hide();
            }
        }

        private void Failed()
        {
            if (!cancelled) IsClicked = false;
        }

        private void Succeeded()
        {
            if (cancelled) return;
            player.SetMoreWild(rules.GetMoreWild());
            view.Hide();
        }
    }
}
