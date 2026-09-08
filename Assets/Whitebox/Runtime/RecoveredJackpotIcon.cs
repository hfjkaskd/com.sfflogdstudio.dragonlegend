using System;
using UnityEngine;
namespace DragonLegend.Whitebox
{
    public sealed class RecoveredJackpotIcon:MonoBehaviour
    {
        [SerializeField] private Animation player;
        [SerializeField] private RecoveredRegionRig rig;
        [SerializeField] private RecoveredRigAnimation idle,win;
        [SerializeField] private float poseTime;
        private bool winning;
        private Action completed;
        private float[] setupBones;
        private Color[] setupColors;
        private float[] setupAttachments;
        public RecoveredRegionRig Rig=>rig;
        public bool IsWinning=>winning;
        private void Awake()
        {
            setupBones=new float[rig.bones.Length*5];setupColors=new Color[rig.slots.Length];setupAttachments=new float[rig.slots.Length];
            for(int i=0;i<rig.bones.Length;i++){var b=rig.bones[i];int at=i*5;setupBones[at]=b.rotation;setupBones[at+1]=b.x;setupBones[at+2]=b.y;setupBones[at+3]=b.scaleX;setupBones[at+4]=b.scaleY;}
            for(int i=0;i<rig.slots.Length;i++){setupColors[i]=rig.slots[i].tint;setupAttachments[i]=rig.slots[i].attachment;}
        }
        public void Sample(bool isWin,float time)
        {
            // PlaySpineAnim forces Initialize(true) and clears tracks. Missing properties
            // belong to the setup pose, never to a previously played win/idle clip.
            for(int i=0;i<rig.bones.Length;i++){var b=rig.bones[i];int at=i*5;b.rotation=setupBones[at];b.x=setupBones[at+1];b.y=setupBones[at+2];b.scaleX=setupBones[at+3];b.scaleY=setupBones[at+4];}
            for(int i=0;i<rig.slots.Length;i++){rig.slots[i].tint=setupColors[i];rig.slots[i].attachment=setupAttachments[i];rig.slots[i].sequenceIndex=-1;}
            (isWin?win:idle).Sample(time,rig);
        }
        private void OnDidApplyAnimationProperties(){Sample(winning,poseTime);}
        public void PlayIdle(){winning=false;completed=null;poseTime=0;Sample(false,0);player.Play("idle");}
        public void PlayWin(Action callback){winning=true;completed=callback;poseTime=0;Sample(true,0);player.Play("win");}
        private void LateUpdate()
        {
            if(!winning||player.IsPlaying("win"))return;
            var callback=completed;PlayIdle();callback?.Invoke();
        }
        public void Cancel(){completed=null;winning=false;if(player!=null)player.Stop();}
        private void OnDisable()=>Cancel();
    }
}
