using System;
using DragonLegend.Whitebox.Recovered;

namespace DragonLegend.Whitebox
{
    public interface IRecoveredMoreSpinView
    {
        void PlaySound(string sound);
        void ShowLimitTip();
        void Hide();
    }

    // UIMoreSpinView 23d55d4 / 23d5720 / 23d5864 / 23d5b2c / 23d5c2c.
    public sealed class RecoveredMoreSpinClaim
    {
        private readonly RecoveredGameplayRules rules;
        private readonly RecoveredPlayerProgress player;
        private readonly PlayerData data;
        private readonly IAdFacade ads;
        private readonly IRecoveredMoreSpinView view;
        private bool cancelled;
        public bool IsClicked {get;private set;}
        public RecoveredMoreSpinClaim(RecoveredGameplayRules config,RecoveredPlayerProgress progress,PlayerData record,IAdFacade facade,IRecoveredMoreSpinView presenter)
        {
            rules=config??throw new ArgumentNullException(nameof(config));player=progress??throw new ArgumentNullException(nameof(progress));
            data=record??throw new ArgumentNullException(nameof(record));ads=facade??throw new ArgumentNullException(nameof(facade));view=presenter??throw new ArgumentNullException(nameof(presenter));
        }
        public int BeforeShow(){cancelled=false;IsClicked=false;view.PlaySound("remind");return rules.GetAddSpins();}
        public void Cancel()=>cancelled=true;
        public void Click(string name)
        {
            if(cancelled||IsClicked)return;
            IsClicked=true;
            if(name=="ClaimBtn") {
                view.PlaySound("click");
                if(data.LimitSpinCount>=rules.GetLimitMaxSpinCount()&&rules.GetConfigType()!="default") {view.ShowLimitTip();return;}
                ads.PlayRewardAd(Succeeded,()=>{if(!cancelled)IsClicked=false;},"extraspin","extraspin");
            }else if(name=="CloseBtn") {view.PlaySound("click");view.Hide();}
        }
        private void Succeeded()
        {
            if(cancelled)return;
            // Native reads the live balance and configuration at success, not show time.
            player.SetSpinCount(unchecked(player.SpinCount+rules.GetAddSpins()));view.Hide();
        }
    }
}
