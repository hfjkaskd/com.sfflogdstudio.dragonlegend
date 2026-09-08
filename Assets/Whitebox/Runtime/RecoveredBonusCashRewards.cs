using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Cash-only continuation of BonusItem.PlayBonusAnim (2396d54).
    public sealed class RecoveredBonusCashRewards : MonoBehaviour
    {
        [SerializeField] private float popupDelay=.2f;
        private sealed class Job
        {
            public RecoveredBonusItemTurn item;
            public Action release;
            public Action<Exception> failed;
            public RecoveredReelWait wait;
            public bool alive=true;
        }
        private readonly List<Job> jobs=new List<Job>();
        public int PendingCount=>jobs.Count;
        public event Action<string> SoundRequested;
        // Original event 1 payload: amount, null callback, source item transform.
        public event Action<float,Action,Transform> FlyCoinRequested;
        public event Action<float,Action<float>> RewardPopupRequested;
        public event Action<Exception> Failed;

        public void Begin(RecoveredBonusItemTurn item,RecoveredGameplayRules rules,int language,Action releaseInput)
        {
            if(item==null||rules==null)throw new ArgumentNullException();
            var job=new Job{item=item,release=releaseInput};
            job.failed=error=>Fail(job,error);item.Failed+=job.failed;jobs.Add(job);
            try {
                SoundRequested?.Invoke("coinReveal");
                if(job.alive)item.Begin(RecoveredBonusType.Reward,rules,language,()=>Turned(job));
            }catch(Exception error){Fail(job,error);}
        }
        private void Turned(Job job)
        {
            if(!job.alive)return;
            // Snapshot before a callback can start another operation on this item.
            float amount=job.item.Reward;
            if(!job.item.RewardJump){
                var source=job.item.transform;var release=job.release;Finish(job);
                release?.Invoke();FlyCoinRequested?.Invoke(amount,null,source);
                return;
            }
            job.wait=RecoveredReelWait.Delay(popupDelay,()=>{
                job.wait=null;if(!job.alive)return;
                RewardPopupRequested?.Invoke(amount,_=>{
                    if(!job.alive)return;
                    var release=job.release;Finish(job);release?.Invoke();
                });
            },error=>Fail(job,error));
        }
        private void Finish(Job job)
        {
            job.alive=false;job.wait?.Cancel();job.wait=null;job.release=null;
            job.item.Failed-=job.failed;jobs.Remove(job);
        }
        private void Fail(Job job,Exception error)
        {
            if(job.alive){Finish(job);job.item.Cancel();}
            Failed?.Invoke(error);
        }
        public void Cancel()
        {
            while(jobs.Count>0){var job=jobs[jobs.Count-1];Finish(job);job.item.Cancel();}
        }
        private void OnDisable()=>Cancel();
    }
}
