using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCoinStopEffect : MonoBehaviour
    {
        [SerializeField] private RecoveredCoinIdle appearance;
        [SerializeField] private RecoveredCoinReveal reveal;
        [SerializeField] private RecoveredCoinGlow glow;
        [SerializeField] private RecoveredCoinRewardText rewardText;
        public RecoveredCoinRewardText RewardText => rewardText;
        public RecoveredCoinReveal Reveal => reveal;
        public RecoveredCoinGlow Glow => glow;
        public event System.Action RevealSoundRequested;
        [SerializeField] private float scaleDuration;
        [SerializeField] private float peakScale;
        [SerializeField] private float restingScale;
        private Vector3 from;
        private float elapsed;
        private int phase;
        public bool IsScaling => phase != 0;
        private void Awake() { reveal.Revealed += RevealCompleted; }
        private void RevealCompleted() => glow.Play();
        public void PlayRewardReveal(float reward, int languageType)
        {
            glow.gameObject.SetActive(false);
            rewardText.Hide();
            RevealSoundRequested?.Invoke();
            appearance.gameObject.SetActive(false);
            reveal.gameObject.SetActive(true);
            reveal.PlayReveal();
            rewardText.Begin(reward, languageType);
        }
        public void PlayShow()
        {
            rewardText.Hide();
            reveal.gameObject.SetActive(false); glow.gameObject.SetActive(false);
            appearance.gameObject.SetActive(true);
            appearance.PlayAppearance();
            from=transform.localScale;elapsed=0;phase=1;
        }
        private void Update()
        {
            if(phase==0)return;
            elapsed+=Time.deltaTime;
            float t=scaleDuration==0?1:Mathf.Clamp01(elapsed/scaleDuration);
            float eased=1-(1-t)*(1-t);
            transform.localScale=Vector3.LerpUnclamped(from,Vector3.one*(phase==1?peakScale:restingScale),eased);
            if(t<1)return;
            if(phase==1){from=transform.localScale;elapsed=0;phase=2;}
            else phase=0;
        }
        private void OnDisable(){phase=0;}
        private void OnDestroy() { if(reveal!=null)reveal.Revealed-=RevealCompleted; }
    }
}
