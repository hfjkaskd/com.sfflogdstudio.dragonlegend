using System;
using UnityEngine;
namespace DragonLegend.Whitebox
{
    // Shared native Animation clock for multi-animation popup artwork.
    public sealed class RecoveredRegionAnimator:MonoBehaviour
    {
        [Serializable] public sealed class Pose {public string clip;public RecoveredRigAnimation animation;}
        [SerializeField] private Animation player;
        [SerializeField] private RecoveredRegionRig rig;
        [SerializeField] private Pose[] poses;
        [SerializeField] private float poseTime;
        [SerializeField] private float playbackSpeed=1;
        private int selected;
        private AnimationState once;
        private Action completed;
        private float[] bones,attachments;
        private Color[] colors;
        public RecoveredRegionRig Rig=>rig;
        public int Selected=>selected;
        public float PlaybackSpeed
        {
            get=>playbackSpeed;
            set {playbackSpeed=value;if(player!=null&&poses!=null&&poses.Length>0)player[poses[selected].clip].speed=value;}
        }
        private void Awake()
        {
            bones=new float[rig.bones.Length*7];colors=new Color[rig.slots.Length];attachments=new float[rig.slots.Length];
            for(int i=0;i<rig.bones.Length;i++){var b=rig.bones[i];int p=i*7;bones[p]=b.rotation;bones[p+1]=b.x;bones[p+2]=b.y;bones[p+3]=b.scaleX;bones[p+4]=b.scaleY;bones[p+5]=b.shearX;bones[p+6]=b.shearY;}
            for(int i=0;i<rig.slots.Length;i++){colors[i]=rig.slots[i].tint;attachments[i]=rig.slots[i].attachment;}
        }
        public void Sample(int index,float time)
        {
            for(int i=0;i<rig.bones.Length;i++){var b=rig.bones[i];int p=i*7;b.rotation=bones[p];b.x=bones[p+1];b.y=bones[p+2];b.scaleX=bones[p+3];b.scaleY=bones[p+4];b.shearX=bones[p+5];b.shearY=bones[p+6];}
            for(int i=0;i<rig.slots.Length;i++){rig.slots[i].tint=colors[i];rig.slots[i].attachment=attachments[i];rig.slots[i].sequenceIndex=-1;}
            poses[index].animation.Sample(time,rig);
        }
        public void Play(int index){once=null;completed=null;selected=index;poseTime=0;Sample(index,0);player[poses[index].clip].wrapMode=WrapMode.Loop;player[poses[index].clip].speed=playbackSpeed;player.Play(poses[index].clip);}
        public void PlayOnce(int index,Action onComplete)
        {
            Play(index);once=player[poses[index].clip];once.wrapMode=WrapMode.ClampForever;completed=onComplete;
        }
        public void Stop(){once=null;completed=null;if(player!=null)player.Stop();}
        private void LateUpdate()
        {
            if(once==null||once.time<once.length)return;
            Sample(selected,once.length);once=null;var callback=completed;completed=null;callback?.Invoke();
        }
        private void OnDidApplyAnimationProperties()=>Sample(selected,poseTime);
        private void OnEnable()=>Play(0);
        private void OnDisable()=>Stop();
    }
}
