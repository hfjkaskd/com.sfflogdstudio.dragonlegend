using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredWildLight : MonoBehaviour
    {
        [SerializeField] private Animation player;
        [SerializeField] private RecoveredWorldRig rig;
        [SerializeField] private string clipName;
        public bool IsPlaying { get; private set; }
        public event Action<RecoveredWildLight> Completed;
        public void Play()
        {
            player.Stop();rig.Sample(0);IsPlaying=true;player.Play(clipName);
        }
        private void LateUpdate()
        {
            if(!IsPlaying||player.IsPlaying(clipName))return;
            IsPlaying=false;Completed?.Invoke(this);
        }
        private void OnDisable() {IsPlaying=false;if(player!=null)player.Stop();}
    }
}
