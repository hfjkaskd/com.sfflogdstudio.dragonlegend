using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Delay/WaitUntil items share the recovered Update runner with SetStop.
    internal sealed class RecoveredReelWait : IRecoveredReelUpdateItem
    {
        private readonly Func<bool> predicate;
        private readonly Action continuation;
        private readonly Action<Exception> failed;
        private readonly float delay;
        private readonly int initialFrame;
        private float elapsed;
        private RecoveredReelWait(float seconds, Func<bool> condition, Action completed, Action<Exception> error)
        {
            delay = (float)TimeSpan.FromMilliseconds(seconds * 1000f).TotalSeconds;
            if (delay < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            predicate = condition; continuation = completed; failed = error; initialFrame = Time.frameCount;
            RecoveredReelStopLoop.Requeue(this);
        }
        internal static void Delay(float seconds, Action completed, Action<Exception> error)
            => new RecoveredReelWait(seconds, null, completed, error);
        internal static void Until(Func<bool> condition, Action completed, Action<Exception> error)
            => new RecoveredReelWait(0, condition, completed, error);
        bool IRecoveredReelUpdateItem.Step(int frame, float scaledDeltaTime)
        {
            try {
                if (predicate != null) { if (!predicate()) return true; }
                else {
                    if (elapsed == 0 && frame == initialFrame) return true;
                    elapsed += scaledDeltaTime;
                    if (elapsed < delay) return true;
                }
                continuation();
            } catch (Exception error) { failed(error); }
            return false;
        }
    }
}
