using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredScatterEffect : MonoBehaviour
    {
        [SerializeField] private Animation player;
        [SerializeField] private RecoveredWorldRig rig;
        [SerializeField] private SortingGroup sorting;
        [SerializeField] private RecoveredWorldRigData idle,start,png;
        private AnimationState once;
        private Action completed;
        public RecoveredWorldRig Rig=>rig;
        public Animation Player=>player;
        public void SetOrder(int order)=>sorting.sortingOrder=order;
        public void PlayStart(Action callback=null)=>Play(start,"start",false,callback);
        public void PlayIdle(bool loop=false)=>Play(idle,"idle",loop,null);
        public void ShowPng(){Stop();rig.SelectClipData(png);}
        private void Play(RecoveredWorldRigData data,string clip,bool loop,Action callback)
        {
            Stop();rig.SelectClipData(data);var state=player[clip];state.time=0;state.speed=1;
            state.wrapMode=loop?WrapMode.Loop:WrapMode.ClampForever;player.Play(clip);
            if(!loop){once=state;completed=callback;}
        }
        private void LateUpdate()
        {
            if(once==null||once.time<once.length)return;
            rig.Sample(once.length);once=null;var callback=completed;completed=null;callback?.Invoke();
        }
        private void Stop(){once=null;completed=null;player.Stop();}
        private void OnEnable()=>PlayIdle(true);
        private void OnDisable()=>Stop();
    }
}
