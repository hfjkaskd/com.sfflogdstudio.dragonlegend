using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Native Animation drives poseTime; each authored clip supplies its source timelines.
    public sealed class RecoveredWorldAnimation : MonoBehaviour
    {
        [Serializable] private sealed class Clip
        {
            public string name;
            public RecoveredWorldRigData data;
        }
        [SerializeField] private Animation player;
        [SerializeField] private RecoveredWorldRig rig;
        [SerializeField] private Clip[] clips;
        [SerializeField] private string initialClip;
        private AnimationState once;
        private Action completed;
        public RecoveredWorldRig Rig=>rig;
        public Animation Player=>player;
        public void Play(string name,bool loop,Action callback=null)
        {
            Clip clip=null;
            for(int i=0;i<clips.Length;i++)if(clips[i].name==name){clip=clips[i];break;}
            if(clip==null)throw new ArgumentOutOfRangeException(nameof(name),name,"No authored world animation.");
            Stop();rig.SelectClipData(clip.data);
            var state=player[name];state.time=0;state.speed=1;
            state.wrapMode=loop?WrapMode.Loop:WrapMode.ClampForever;player.Play(name);
            if(!loop){once=state;completed=callback;}
        }
        public void Stop(){once=null;completed=null;player.Stop();}
        private void LateUpdate()
        {
            if(once==null || once.time<once.length)return;
            rig.Sample(once.length);once=null;var callback=completed;completed=null;callback?.Invoke();
        }
        private void OnEnable()=>Play(initialClip,true);
        private void OnDisable()=>Stop();
    }
}
