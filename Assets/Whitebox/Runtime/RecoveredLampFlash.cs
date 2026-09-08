using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredLampFlash : MonoBehaviour
    {
        [SerializeField] private Animation animationPlayer;
        [SerializeField] private AnimationClip setup;
        public bool IsPlaying { get; private set; }
        public event Action<RecoveredLampFlash> Completed;
        public void Play()
        {
            animationPlayer.Stop(); setup.SampleAnimation(gameObject, 0);
            IsPlaying = true; animationPlayer.Play("dianliang");
        }
        private void LateUpdate()
        {
            if (!IsPlaying || animationPlayer.IsPlaying("dianliang")) return;
            IsPlaying = false; Completed?.Invoke(this);
        }
        private void OnDisable() { IsPlaying = false; if(animationPlayer!=null)animationPlayer.Stop(); }
    }
}
