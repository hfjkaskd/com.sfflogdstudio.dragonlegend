using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCoinGlow : MonoBehaviour
    {
        [SerializeField] private Animation animationPlayer;
        [SerializeField] private AnimationClip setup;
        public bool IsPlaying { get; private set; }
        public void Play()
        {
            gameObject.SetActive(true);
            animationPlayer.Stop(); setup.SampleAnimation(gameObject, 0);
            IsPlaying = true; animationPlayer.Play("glow");
        }
        private void LateUpdate()
        {
            if (!IsPlaying || animationPlayer.IsPlaying("glow")) return;
            IsPlaying = false; gameObject.SetActive(false);
        }
        private void OnDisable() { IsPlaying = false; if (animationPlayer != null) animationPlayer.Stop(); }
    }
}
