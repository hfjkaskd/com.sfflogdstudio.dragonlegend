using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCoinIdle : MonoBehaviour
    {
        [SerializeField] private Animation animationPlayer;
        [SerializeField] private AnimationClip setup;
        private bool initialized;
        public bool IsShowing { get; private set; }
        private void Start() { initialized = true; if (!IsShowing) Play(); }
        private void OnEnable() { if (initialized) Play(); }
        private void Play()
        {
            IsShowing = false;
            animationPlayer.Stop();
            setup.SampleAnimation(gameObject, 0);
            animationPlayer.Play("zcjb_idle");
        }
        // JinBiEffectItem's appearance clip completion reinitializes before idle.
        // The parent effect's scale tween, label and glow are separate responsibilities.
        public void PlayAppearance()
        {
            if (animationPlayer.GetClip("zcjb_chuxian") == null)
                throw new System.InvalidOperationException("This prefab has no appearance clip.");
            animationPlayer.Stop();
            setup.SampleAnimation(gameObject, 0);
            IsShowing = true;
            animationPlayer.Play("zcjb_chuxian");
        }
        private void LateUpdate()
        {
            if (IsShowing && !animationPlayer.IsPlaying("zcjb_chuxian")) Play();
        }
    }
}
