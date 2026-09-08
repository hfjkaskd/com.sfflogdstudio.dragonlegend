using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCoinIdle : MonoBehaviour
    {
        [SerializeField] private Animation animationPlayer;
        [SerializeField] private AnimationClip setup;
        private bool initialized;
        private void Start() { initialized = true; Play(); }
        private void OnEnable() { if (initialized) Play(); }
        private void Play()
        {
            animationPlayer.Stop();
            setup.SampleAnimation(gameObject, 0);
            animationPlayer.Play("zcjb_idle");
        }
    }
}
