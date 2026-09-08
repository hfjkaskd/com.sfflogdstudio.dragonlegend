using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Base-mode movement from StarSlotSpin, ConstantSpeedRoll, StopSlotRoll.
    // Free stop presentation remains a separate unfinished branch.
    public sealed class RecoveredBaseReelMotion : MonoBehaviour
    {
        [SerializeField] private RecoveredReelView reel;
        [SerializeField] private float stopOvershoot;
        private int phase; // idle, accelerating, constant, one-frame stop wait, return tween
        private float maxSpeed, accelerationTarget, duration, elapsed, returnStart;
        private Func<IReadOnlyList<int>> resultProvider;
        public bool IsSpinning { get; private set; }
        public bool StopRequested { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float ReturnDuration { get; private set; }
        public bool StartSlotSpin(float speedPixelsPerSecond, float accelerationSeconds)
        {
            if (IsSpinning) { Debug.LogError("Already Start"); return false; }
            maxSpeed = accelerationTarget = speedPixelsPerSecond;
            duration = Mathf.Max(0, accelerationSeconds); elapsed = 0; CurrentSpeed = 0;
            phase = 1; IsSpinning = true;
            return true;
        }
        public void SetMaxSpeed(float speedPixelsPerSecond) => maxSpeed = speedPixelsPerSecond;
        public void RequestStop(Func<IReadOnlyList<int>> currentColumn)
        {
            resultProvider = currentColumn ?? throw new ArgumentNullException(nameof(currentColumn));
            StopRequested = true;
        }
        public RecoveredReelStopOperation SetStop(float seconds, Func<IReadOnlyList<int>> currentColumn,
            Action<RecoveredReelView> completed = null)
            => RecoveredReelStopLoop.Schedule(this,reel,seconds,currentColumn,completed);
        private void Update() => AdvanceMotion(Time.deltaTime);
        public void AdvanceMotion(float deltaTime)
        {
            if (phase == 1) {
                elapsed += deltaTime;
                float t = duration == 0 ? 1 : Mathf.Clamp01(elapsed / duration);
                CurrentSpeed = accelerationTarget * (float)Math.Sin(t * Math.PI * 0.5);
                reel.Refresh(CurrentSpeed, deltaTime, false);
                if (t < 1) return;
                phase = 2;
                // StartCoroutine executes its first iteration in this same completion callback.
                AdvanceConstant(deltaTime);
            }
            else if (phase == 2) AdvanceConstant(deltaTime);
            else if (phase == 3) {
                phase = 0; // A failed result callback terminates this coroutine stage.
                reel.ApplyBaseColumn(resultProvider());
                returnStart = reel.OffsetPixels;
                ReturnDuration = Mathf.Abs(returnStart) * 2 / maxSpeed;
                duration = Mathf.Max(0, ReturnDuration); elapsed = 0; phase = 4;
            }
            else if (phase == 4) {
                elapsed += deltaTime;
                float t = duration == 0 ? 1 : Mathf.Clamp01(elapsed / duration);
                float x = t - 1;
                float eased = x * x * ((stopOvershoot + 1) * x + stopOvershoot) + 1;
                reel.SetOffsetPixels(returnStart * (1 - eased));
                if (t >= 1) {
                    reel.SetOffsetPixels(0);
                    phase = 0; IsSpinning = false; StopRequested = false;
                }
            }
        }
        private void AdvanceConstant(float deltaTime)
        {
            if (StopRequested) { phase = 3; return; }
            CurrentSpeed = maxSpeed; reel.Refresh(maxSpeed, deltaTime, true);
        }
    }
}
