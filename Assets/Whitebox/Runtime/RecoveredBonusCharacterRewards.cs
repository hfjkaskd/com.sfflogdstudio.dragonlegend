using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace DragonLegend.Whitebox
{
    // Character branch of BonusItem.PlayBonusAnim. Multiple flights may overlap
    // because non-completing characters release input before flight starts.
    public sealed class RecoveredBonusCharacterRewards : MonoBehaviour
    {
        [SerializeField] private RecoveredLampFlight flightPrefab;
        [SerializeField] private RecoveredLampFlash flashPrefab;
        [SerializeField] private float jackpotDelay=1.5f;
        private sealed class Job
        {
            public RecoveredBonusItemTurn item;
            public RecoveredBonusRound.Reveal reveal;
            public Transform target;
            public RecoveredPlayerProgress progress;
            public Action release;
            public RecoveredLampFlight flight;
            public RecoveredLampFlash flash;
            public RecoveredReelWait wait;
            public int canvasOrder;
            public bool alive=true;
        }
        private readonly List<Job> jobs=new List<Job>();
        private readonly Dictionary<RecoveredLampFlight,Job> flying=new Dictionary<RecoveredLampFlight,Job>();
        private readonly Dictionary<RecoveredLampFlash,Job> flashing=new Dictionary<RecoveredLampFlash,Job>();
        private ObjectPool<RecoveredLampFlight> flights;
        private ObjectPool<RecoveredLampFlash> flashes;
        public int PendingCount=>jobs.Count;
        public int ActiveFlightCount=>flying.Count;
        public int ActiveFlashCount=>flashing.Count;
        public event Action<string> SoundRequested;
        public event Action<int> VibrationRequested;
        public event Action<RecoveredJackpotType> JackpotAnimationRequested;
        public event Action<RecoveredJackpotType,float,Action<float>> JackpotPopupRequested;
        public event Action<RecoveredLampFlight> FlightStarted;
        public event Action<Exception> Failed;
        private void Awake()
        {
            flights=new ObjectPool<RecoveredLampFlight>(CreateFlight,null,v=>v.gameObject.SetActive(false),v=>Destroy(v.gameObject));
            flashes=new ObjectPool<RecoveredLampFlash>(CreateFlash,null,v=>v.gameObject.SetActive(false),v=>Destroy(v.gameObject));
        }
        private RecoveredLampFlight CreateFlight(){var value=Instantiate(flightPrefab,transform,false);value.Arrived+=Arrived;return value;}
        private RecoveredLampFlash CreateFlash(){var value=Instantiate(flashPrefab,transform,false);value.Completed+=Flashed;return value;}
        public void Begin(RecoveredBonusItemTurn item,RecoveredBonusRound.Reveal reveal,Transform target,
            RecoveredPlayerProgress progress,RecoveredGameplayRules rules,int language,int canvasOrder,Action releaseInput)
        {
            if(reveal.type==RecoveredBonusType.Reward)throw new ArgumentException("Cash card uses the Reward branch",nameof(reveal));
            if(item==null||progress==null||rules==null)throw new ArgumentNullException();
            var job=new Job{item=item,reveal=reveal,target=target,progress=progress,release=releaseInput,canvasOrder=canvasOrder};jobs.Add(job);
            try {
                if(reveal.completesJackpot)progress.SetTaskData(2,1);
                SoundRequested?.Invoke("coinReveal");
                if(job.alive)item.Begin(reveal.type,rules,language,()=>{try{Turned(job);}catch(Exception error){Fail(job,error);}});
            }catch(Exception error){Fail(job,error);}
        }
        private static void ReleaseInput(Job job){var callback=job.release;job.release=null;callback?.Invoke();}
        private void Turned(Job job)
        {
            if(!job.alive)return;
            if(job.target==null){ReleaseInput(job);Finish(job);return;}
            if(!job.reveal.completesJackpot)ReleaseInput(job);
            if(!job.alive)return;
            VibrationRequested?.Invoke(200);if(!job.alive)return;
            var flight=flights.Get();job.flight=flight;flying.Add(flight,job);
            flight.transform.SetParent(job.item.transform,false);
            flight.transform.localPosition=flightPrefab.transform.localPosition;
            flight.transform.localRotation=flightPrefab.transform.localRotation;
            flight.transform.localScale=flightPrefab.transform.localScale;
            flight.gameObject.SetActive(true);flight.Begin(job.target,job.canvasOrder);FlightStarted?.Invoke(flight);
        }
        private void Arrived(RecoveredLampFlight flight)
        {
            if(!flying.TryGetValue(flight,out var job))return;
            try {
                SoundRequested?.Invoke("exp");if(!job.alive)return;
                flying.Remove(flight);job.flight=null;flights.Release(flight);
                job.target.GetChild(0).gameObject.SetActive(false);
                var flash=flashes.Get();job.flash=flash;flashing.Add(flash,job);
                flash.transform.SetParent(job.target,false);flash.transform.localScale=flashPrefab.transform.localScale;
                ((RectTransform)flash.transform).anchoredPosition=Vector2.zero;flash.gameObject.SetActive(true);
                JackpotAnimationRequested?.Invoke(job.reveal.jackpot);
                if(job.alive)flash.Play();
            }catch(Exception error){Fail(job,error);}
        }
        private void Flashed(RecoveredLampFlash flash)
        {
            if(!flashing.TryGetValue(flash,out var job))return;
            flashing.Remove(flash);job.flash=null;flashes.Release(flash);
            if(!job.reveal.completesJackpot){Finish(job);return;}
            float amount=job.reveal.jackpot==RecoveredJackpotType.Grand?job.progress.GrandJackPotReward:
                job.reveal.jackpot==RecoveredJackpotType.Major?job.progress.MajorJackPotReward:job.progress.MiniJackPotReward;
            job.wait=RecoveredReelWait.Delay(jackpotDelay,()=>{
                job.wait=null;if(!job.alive)return;
                JackpotPopupRequested?.Invoke(job.reveal.jackpot,amount,_=>{
                    if(!job.alive)return;ReleaseInput(job);Finish(job);
                });
            },error=>Fail(job,error));
        }
        private void Finish(Job job){job.alive=false;job.wait?.Cancel();job.wait=null;jobs.Remove(job);}
        private void Fail(Job job,Exception error){Cancel(job);Failed?.Invoke(error);}
        private void Cancel(Job job)
        {
            Finish(job);job.item.Cancel();job.release=null;
            if(job.flight!=null){flying.Remove(job.flight);flights.Release(job.flight);job.flight=null;}
            if(job.flash!=null){flashing.Remove(job.flash);flashes.Release(job.flash);job.flash=null;}
        }
        public void Cancel(){while(jobs.Count>0)Cancel(jobs[jobs.Count-1]);}
        private void OnDisable()=>Cancel();
        private void OnDestroy(){Cancel();flights?.Clear();flashes?.Clear();}
    }
}
