using System;
using UnityEngine;
namespace DragonLegend.Whitebox
{
    public sealed class RecoveredWinBurst:MonoBehaviour
    {
        [SerializeField] private Animation player;
        [SerializeField] private AnimationClip setup;
        [SerializeField] private RecoveredRegionRig rig;
        [SerializeField] private RecoveredRigAnimation poses;
        [SerializeField] private float poseTime;
        private void OnDidApplyAnimationProperties(){if(poses!=null)poses.Sample(poseTime,rig);}
        public bool IsPlaying {get;private set;}
        public event Action<RecoveredWinBurst> Completed;
        public void Play(){player.Stop();setup.SampleAnimation(gameObject,0);poses.Sample(0,rig);IsPlaying=true;player.Play("animation");}
        private void LateUpdate(){if(!IsPlaying||player.IsPlaying("animation"))return;IsPlaying=false;Completed?.Invoke(this);}
        private void OnDisable(){IsPlaying=false;if(player!=null)player.Stop();}
    }
}
