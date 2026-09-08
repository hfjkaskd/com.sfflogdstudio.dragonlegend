using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCoinReveal : MonoBehaviour
    {
        [SerializeField] private Animation animationPlayer;
        [SerializeField] private AnimationClip setup;
        [SerializeField] private float revealSpeed = 3;
        public bool IsRevealing { get; private set; }
        public event Action Revealed;

        public void PlayReveal()
        {
            animationPlayer.Stop();
            setup.SampleAnimation(gameObject, 0);
            animationPlayer["zcjb_b_chun"].speed = revealSpeed;
            IsRevealing = true;
            animationPlayer.Play("zcjb_b_chun");
        }

        private void LateUpdate()
        {
            if (!IsRevealing || animationPlayer.IsPlaying("zcjb_b_chun")) return;
            IsRevealing = false;
            // Native completion restores timeScale=1 and reinitializes before idle_chun.
            animationPlayer.Stop();
            setup.SampleAnimation(gameObject, 0);
            animationPlayer["idle_chun"].speed = 1;
            animationPlayer.Play("idle_chun");
            Revealed?.Invoke();
        }

        private void OnDisable()
        {
            IsRevealing = false;
            animationPlayer.Stop();
        }
    }
}
