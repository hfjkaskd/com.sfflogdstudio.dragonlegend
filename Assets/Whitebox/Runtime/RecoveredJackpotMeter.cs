using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DragonLegend.Whitebox
{
    public sealed class RecoveredJackpotMeter:MonoBehaviour
    {
        [SerializeField] private RecoveredJackpotIcon icon;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private Text greenRewardText;
        [SerializeField] private float rewardDuration;
        private RecoveredPlayerProgress progress;
        private RecoveredGameplayRules rules;
        private Func<int> readBet;
        private int index,language;
        private float multiplier,currentReward,target,elapsed,start;
        private bool animating,started;
        public Text Label=>greenRewardText;
        public RecoveredJackpotIcon Icon=>icon;
        public float CurrentReward=>currentReward;
        public bool IsAnimating=>animating;
        public void Initialize(int typeIndex,float fan,RecoveredPlayerProgress data,RecoveredGameplayRules config,Func<int> bet,int languageType)
        {
            Cancel();index=typeIndex;multiplier=fan;progress=data;rules=config;readBet=bet;language=languageType;
            icon.PlayIdle();rewardText.gameObject.SetActive(false);RefreshRewardValue();
        }
        private float Reward()=>rules.GetJackpotReward(multiplier,readBet(),progress.JpAddCount);
        private void Store(float value)
        {
            switch(index){case 0:progress.GrandJackPotReward=value;break;case 1:progress.MajorJackPotReward=value;break;case 2:progress.MiniJackPotReward=value;break;}
        }
        public void RefreshRewardValue()
        {
            float value=Reward();string text=RecoveredCurrency.Format(value,language,2);
            rewardText.text=text;greenRewardText.text=text;currentReward=value;Store(value);
        }
        public void PlayRewardAnim()
        {
            target=Reward();Store(target);animating=true;started=false;elapsed=0;
        }
        private void Update()
        {
            if(!animating)return;
            if(!started){start=currentReward;started=true;}
            elapsed+=Time.deltaTime;
            float t=rewardDuration==0?1:Mathf.Clamp01(elapsed/rewardDuration);
            float value=start+(target-start)*(1-(1-t)*(1-t));
            // Native tween setter changes only the visible Text, not curJackPotWin.
            greenRewardText.text=RecoveredCurrency.Format(value,language,2);
            if(t==1)animating=false;
        }
        public void PlayAnim(Action callback)=>icon.PlayWin(()=>{callback?.Invoke();RefreshRewardValue();});
        public void Cancel(){animating=false;if(icon!=null)icon.Cancel();}
        private void OnDisable()=>Cancel();
    }
}
