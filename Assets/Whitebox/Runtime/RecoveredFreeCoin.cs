using System.Collections.Generic;
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
        private struct ScaleJob {public Vector3 from;public float elapsed;public bool started,returning,done;}
        private readonly List<ScaleJob> scales=new List<ScaleJob>(4);
        public RecoveredWorldAnimation Art=>art;
        public RecoveredWorldAnimation Glow=>glow;
        public Text Reward=>reward;
        public bool IsScaling=>scales.Count!=0;

        public void PlayShow()
        {
            glow.gameObject.SetActive(false);reward.gameObject.SetActive(false);
            art.Play("zcjb_chuxian",false,PlayIdle);
            scales.Add(new ScaleJob());
        }
        private void PlayIdle()=>art.Play("zcjb_idle",true);
        private void Update()=>AdvanceScale(Time.deltaTime);
        public void AdvanceScale(float deltaTime)
        {
            if(deltaTime<.000001f)return;
            // TweenManager snapshots its active upper bound; callback-created return
            // tweens begin next update. Each getter captures scale at lazy startup.
            int count=scales.Count;
            for(int i=0;i<count;i++) {
                var job=scales[i];
                if(!job.started){job.started=true;job.from=transform.localScale;}
                job.elapsed+=deltaTime;
                float t=scaleDuration==0?1:Mathf.Clamp01(job.elapsed/scaleDuration);
                float eased=1-(1-t)*(1-t);
                transform.localScale=Vector3.LerpUnclamped(job.from,Vector3.one*(job.returning?restingScale:peakScale),eased);
                if(t>=1) {
                    job.done=true;
                    if(!job.returning)scales.Add(new ScaleJob{returning=true});
                }
                scales[i]=job;
            }
            int keep=0;
            for(int i=0;i<scales.Count;i++)if(!scales[i].done)scales[keep++]=scales[i];
            if(keep<scales.Count)scales.RemoveRange(keep,scales.Count-keep);
        }
        private void OnDisable(){scales.Clear();}
    }
}
