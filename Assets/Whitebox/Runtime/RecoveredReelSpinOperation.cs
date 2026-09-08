using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // RollReel.<StarSpin>d__23 0x23789bc, base mode only.
    public sealed class RecoveredReelSpinOperation : CustomYieldInstruction, IRecoveredReelUpdateItem
    {
        private readonly RecoveredBaseReelMotion motion;
        private readonly int index;
        private readonly Action<int> stopped;
        private RecoveredReelStopOperation stop;
        public bool IsCompleted { get; private set; }
        public Exception Error { get; private set; }
        public override bool keepWaiting { get { if (IsCompleted) GetResult(); return !IsCompleted; } }

        internal RecoveredReelSpinOperation(RecoveredBaseReelMotion target, RecoveredReelView reel,
            float speed, float stopDelay, float accelerationSeconds, int reelIndex,
            Func<IReadOnlyList<int>> column, Action<int> accelerationCallback,
            Action<int> stoppedCallback, int selectedIndex)
        {
            motion = target; index = reelIndex; stopped = stoppedCallback;
            try {
                reel.ResetPresentation();
                motion.StartSlotSpin(speed, accelerationSeconds);
                if (accelerationCallback != null) {
                    if (index == selectedIndex) accelerationCallback(index);
                    IsCompleted = true;
                } else {
                    stop = motion.SetStop(stopDelay, column);
                    stop.Continuation = AfterStop;
                }
            } catch (Exception error) { Error = error; IsCompleted = true; }
        }
        private void AfterStop()
        {
            try {
                stop.GetResult();
                // A second WaitUntil(!isStop) is queued after awaiting SetStop.
                // It must not run inline or test IsSpinning instead.
                RecoveredReelStopLoop.Requeue(this);
            } catch (Exception error) { Error = error; IsCompleted = true; }
        }
        bool IRecoveredReelUpdateItem.Step(int frame, float scaledDeltaTime)
        {
            if (motion.StopRequested) return true;
            try { stopped?.Invoke(index); }
            catch (Exception error) { Error = error; }
            IsCompleted = true;
            return false;
        }
        public void GetResult()
        {
            if (!IsCompleted) throw new InvalidOperationException("Spin startup operation is not complete.");
            if (Error != null) throw Error;
        }
    }
}
