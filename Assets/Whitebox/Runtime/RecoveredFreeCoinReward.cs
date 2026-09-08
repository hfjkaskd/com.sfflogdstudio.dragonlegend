using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // JinBiEffectItem.PlayAnim 0x23da0c0 and completion 0x23d9b48.
    public sealed class RecoveredFreeCoinReward : MonoBehaviour
    {
        [SerializeField] private RecoveredFreeCoin coin;
        [SerializeField] private RecoveredCoinRewardText rewardText;
        [SerializeField] private string revealClip,idleClip,glowClip,revealSound;
        [SerializeField] private float revealSpeed;
        private Transform destination;
        private Action callback;
        public bool IsRevealing {get;private set;}
        public bool IsPresenting {get;private set;}
        public RecoveredCoinRewardText RewardText=>rewardText;
        public event Action<string> SoundRequested;
        public event Action<RecoveredFreeCoinReward,Transform> LampFlightRequested;
        public event Action PresentationFinished;
        private void Awake()=>rewardText.PresentationFinished+=Finish;
        public void Play(float amount,int language,Transform target,Action completed=null)
        {
            destination=target;callback=completed;IsPresenting=true;IsRevealing=true;
            coin.Glow.gameObject.SetActive(false);rewardText.Hide();
            SoundRequested?.Invoke(revealSound);
            coin.Art.Play(revealClip,false,RevealComplete);coin.Art.Player[revealClip].speed=revealSpeed;
            rewardText.Begin(amount,language);
        }
        private void RevealComplete()
        {
            IsRevealing=false;
            coin.Art.Play(idleClip,true);
            coin.Glow.gameObject.SetActive(true);coin.Glow.Play(glowClip,false,HideGlow);
        }
        private void HideGlow()=>coin.Glow.gameObject.SetActive(false);
        private void Finish()
        {
            if(destination!=null)LampFlightRequested?.Invoke(this,destination);
            IsPresenting=false;var completed=callback;callback=null;completed?.Invoke();PresentationFinished?.Invoke();
        }
        private void OnDisable(){IsRevealing=false;IsPresenting=false;callback=null;destination=null;}
        private void OnDestroy(){if(rewardText!=null)rewardText.PresentationFinished-=Finish;}
    }
}
