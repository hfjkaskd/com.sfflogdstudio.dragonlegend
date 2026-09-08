using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // LongzhuItem.PlayRewardAnim 0x23af730; numeric tween and independent scale tweens.
    public sealed class RecoveredFreeBallReward : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private float delay,duration,scaleDuration,peakScale,restingScale;
        private sealed class Job
        {
            public int phase;
            public float elapsed,amount;
            public bool started,cancelled;
            public Vector3 from;
            public Func<int> language;
        }
        private readonly List<Job> jobs=new List<Job>(4);
        private readonly List<RecoveredReelWait> waits=new List<RecoveredReelWait>(1);
        private Job numeric;
        public bool IsAnimating=>jobs.Count!=0||waits.Count!=0;
        public Exception Error {get;private set;}
        public void Play(float amount,Func<int> currentLanguage,Action scheduled=null)
        {
            // Native kills only the stored numeric tween; pending waits and scale jobs survive.
            if(numeric!=null)numeric.cancelled=true;
            numeric=null;RecoveredReelWait wait=null;
            wait=RecoveredReelWait.Delay(delay,()=>{
                waits.Remove(wait);numeric=new Job{amount=amount,language=currentLanguage};jobs.Add(numeric);scheduled?.Invoke();
            },error=>Error=error);
            waits.Add(wait);
        }
        private void Update()
        {
            if(Time.deltaTime<.000001f)return;
            int count=jobs.Count;
            for(int i=0;i<count;i++) {
                var job=jobs[i];if(job.cancelled)continue;
                if(!job.started){job.started=true;job.from=label.transform.localScale;}
                job.elapsed+=Time.deltaTime;float span=job.phase==0?duration:scaleDuration;
                float t=span==0?1:Mathf.Clamp01(job.elapsed/span),ease=1-(1-t)*(1-t);
                if(job.phase==0)label.text=RecoveredCurrency.Format(job.amount*ease,job.language(),2);
                else label.transform.localScale=Vector3.LerpUnclamped(job.from,Vector3.one*(job.phase==1?peakScale:restingScale),ease);
                if(t<1)continue;
                job.cancelled=true;
                if(job.phase<2)jobs.Add(new Job{phase=job.phase+1});
            }
            for(int i=jobs.Count-1;i>=0;i--)if(jobs[i].cancelled)jobs.RemoveAt(i);
        }
        private void OnDestroy(){foreach(var wait in waits)wait.Cancel();}
    }
}
