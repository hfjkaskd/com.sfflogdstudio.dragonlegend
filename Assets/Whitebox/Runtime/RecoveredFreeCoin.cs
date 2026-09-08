using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // JinBiEffectItem.PlayShowAnim 0x23d95e0 and its two completion callbacks.
    public sealed class RecoveredFreeCoin : MonoBehaviour
    {
        [SerializeField] private RecoveredWorldAnimation art;
        [SerializeField] private RecoveredWorldAnimation glow;
        [SerializeField] private Text reward;
        [SerializeField] private RecoveredWorldRectClip clipping;
        public RecoveredWorldRectClip Clipping=>clipping;
        [SerializeField] private float scaleDuration,peakScale,restingScale;
        private Vector3 from;
        private float elapsed;
        private int phase;
        public RecoveredWorldAnimation Art=>art;
        public RecoveredWorldAnimation Glow=>glow;
        public Text Reward=>reward;
        public bool IsScaling=>phase!=0;

        public void PlayShow()
        {
            glow.gameObject.SetActive(false);reward.gameObject.SetActive(false);
            art.Play("zcjb_chuxian",false,PlayIdle);
            from=transform.localScale;elapsed=0;phase=1;
        }
        private void PlayIdle()=>art.Play("zcjb_idle",true);
        private void Update()
        {
            if(phase==0)return;
            elapsed+=Time.deltaTime;
            float t=scaleDuration==0?1:Mathf.Clamp01(elapsed/scaleDuration);
            float eased=1-(1-t)*(1-t);
            transform.localScale=Vector3.LerpUnclamped(from,Vector3.one*(phase==1?peakScale:restingScale),eased);
            if(t<1)return;
            if(phase==1){from=transform.localScale;elapsed=0;phase=2;}else phase=0;
        }
        private void OnDisable(){phase=0;}
    }
}
