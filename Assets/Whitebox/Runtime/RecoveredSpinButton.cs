using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Original SpinBtn: idle loops; accepted click plays dianji and resets to idle on completion.
    public sealed class RecoveredSpinButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Animation animationPlayer;
        [SerializeField] private AnimationClip setup;
        private bool clicking;
        private bool initialized;
        public Button Button => button;
        public bool IsClickAnimationPlaying => clicking;
        // During Instantiate, the parent's OnEnable precedes child Graphic.Awake.
        // Sample the complete hierarchy only after Unity has initialized those components.
        private void Start() { initialized = true; PlayIdle(); }
        private void OnEnable() { if (initialized) PlayIdle(); }
        public void PlayAcceptedClick()
        {
            animationPlayer.Stop(); setup.SampleAnimation(gameObject, 0);
            clicking = true; animationPlayer.Play("dianji");
        }
        public void PlayIdle()
        {
            animationPlayer.Stop(); setup.SampleAnimation(gameObject, 0);
            clicking = false; animationPlayer.Play("idle");
        }
        private void LateUpdate()
        {
            if (clicking && !animationPlayer.IsPlaying("dianji")) PlayIdle();
        }
    }
}
