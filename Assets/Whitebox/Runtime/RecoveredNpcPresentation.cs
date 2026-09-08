using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredNpcPresentation : MonoBehaviour
    {
        [SerializeField] private RecoveredRegionAnimator dragon,fire;
        [SerializeField] private RecoveredBoardShake shake;
        [SerializeField] private int idleClip,winClip,windClip,fireClip;
        [SerializeField] private float soundDelay,shakeDelay;
        [SerializeField] private string soundName;
        private sealed class Pending {public RecoveredReelWait wait;public bool shake;}
        private readonly List<Pending> pending=new List<Pending>();
        public event Action<string> SoundRequested;
        public event Action<Exception> Failed;
        public int State {get;private set;}
        public RecoveredBoardShake Shake=>shake;
        public void Show(int state,Action completed=null)
        {
            if(state<0||state>2)return;
            State=state;
            if(state==0){dragon.Play(idleClip);fire.gameObject.SetActive(false);return;}
            fire.gameObject.SetActive(true);fire.PlayOnce(state==1?fireClip:windClip,null);
            // Native fire-and-forget audio delays survive subsequent InitNpc calls.
            var item=new Pending{shake=state==2};pending.Add(item);
            item.wait=RecoveredReelWait.Delay(soundDelay,()=>Sound(item),error=>Fail(item,error));
            dragon.PlayOnce(winClip,()=>{
                fire.gameObject.SetActive(false);dragon.Play(idleClip);State=0;completed?.Invoke();
            });
        }
        private void Sound(Pending item)
        {
            item.wait=null;SoundRequested?.Invoke(soundName);
            if(!pending.Contains(item))return;
            item.wait=RecoveredReelWait.Delay(shakeDelay,()=>AfterSound(item),error=>Fail(item,error));
        }
        private void AfterSound(Pending item)
        {
            item.wait=null;pending.Remove(item);if(item.shake)shake.Begin();
        }
        private void Fail(Pending item,Exception error){item.wait?.Cancel();pending.Remove(item);Failed?.Invoke(error);}
        public void Cancel()
        {
            foreach(var item in pending)item.wait?.Cancel();pending.Clear();shake.Cancel();
            if(isActiveAndEnabled)Show(0);
            else {State=0;if(fire!=null)fire.gameObject.SetActive(false);}
        }
        private void OnDisable()=>Cancel();
    }
}
