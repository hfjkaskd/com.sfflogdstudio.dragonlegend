using UnityEngine;

namespace DragonLegend.Whitebox
{
    // FloatBankItem Start 2391498, PlayAnim 23908d0, StopAnim 23909a0.
    public sealed class RecoveredBankFloat : MonoBehaviour
    {
        private Vector3 originalPosition;
        private Motion latest;
        private void Start() => originalPosition = transform.localPosition;
        public void Play(float height, float duration)
        {
            // Native replaces the handle without killing a previous tween.
            latest = new Motion(this, originalPosition.y + height, duration);
            RecoveredReelStopLoop.Requeue(latest);
        }
        public void Stop()
        {
            if (latest == null) return;
            latest.Cancelled = true; latest = null;
            transform.localPosition = originalPosition;
        }
        private sealed class Motion : IRecoveredReelUpdateItem
        {
            private readonly RecoveredBankFloat owner;
            private readonly float target, duration;
            private float start, elapsed;
            private bool initialized;
            public bool Cancelled;
            public Motion(RecoveredBankFloat component, float end, float seconds)
            { owner = component; target = end; duration = seconds; }
            public bool Step(int frame, float scaledDeltaTime)
            {
                if (Cancelled || owner == null) return false;
                if (scaledDeltaTime <= 0) return true;
                var position = owner.transform.localPosition;
                if (!initialized) { start = position.y; initialized = true; }
                elapsed += scaledDeltaTime;
                position.y = Mathf.LerpUnclamped(start, target, Mathf.PingPong(elapsed / duration, 1));
                owner.transform.localPosition = position;
                return true;
            }
        }
    }
}
