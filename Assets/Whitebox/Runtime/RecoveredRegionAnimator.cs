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
        private int selected;
        private float[] bones,attachments;
        private Color[] colors;
        public RecoveredRegionRig Rig=>rig;
        public int Selected=>selected;
        private void Awake()
        {
            bones=new float[rig.bones.Length*5];colors=new Color[rig.slots.Length];attachments=new float[rig.slots.Length];
            for(int i=0;i<rig.bones.Length;i++){var b=rig.bones[i];int p=i*5;bones[p]=b.rotation;bones[p+1]=b.x;bones[p+2]=b.y;bones[p+3]=b.scaleX;bones[p+4]=b.scaleY;}
            for(int i=0;i<rig.slots.Length;i++){colors[i]=rig.slots[i].tint;attachments[i]=rig.slots[i].attachment;}
        }
        public void Sample(int index,float time)
        {
            for(int i=0;i<rig.bones.Length;i++){var b=rig.bones[i];int p=i*5;b.rotation=bones[p];b.x=bones[p+1];b.y=bones[p+2];b.scaleX=bones[p+3];b.scaleY=bones[p+4];}
            for(int i=0;i<rig.slots.Length;i++){rig.slots[i].tint=colors[i];rig.slots[i].attachment=attachments[i];rig.slots[i].sequenceIndex=-1;}
            poses[index].animation.Sample(time,rig);
        }
        public void Play(int index){selected=index;poseTime=0;Sample(index,0);player.Play(poses[index].clip);}
        private void OnDidApplyAnimationProperties()=>Sample(selected,poseTime);
        private void OnEnable()=>Play(0);
        private void OnDisable(){if(player!=null)player.Stop();}
    }
}
