using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;

namespace DragonLegend.Whitebox
{
    // SetStop 0x2378388: scaled Update delay, set flag, wait until !isStop, then callback.
    // Yield this operation in a native Unity coroutine; GetResult propagates callback faults.
    internal interface IRecoveredReelUpdateItem { bool Step(int frame, float scaledDeltaTime); }

    public sealed class RecoveredReelStopOperation : CustomYieldInstruction, IRecoveredReelUpdateItem
    {
        private readonly RecoveredBaseReelMotion motion;
        private readonly RecoveredReelView reel;
        private readonly Func<IReadOnlyList<int>> column;
        private readonly Action<RecoveredReelView> callback;
        private readonly int initialFrame;
        private readonly float delay;
        private float elapsed;
        internal Action Continuation;
        private int phase; // delay, flag wait, invoking, complete
        public bool IsCompleted => phase == 3;
        public Exception Error { get; private set; }
        internal void CancelForProfileChange() { Error = new OperationCanceledException("GM profile changed."); phase = 3; }
        public override bool keepWaiting { get { if (IsCompleted) GetResult(); return !IsCompleted; } }
        internal RecoveredReelStopOperation(RecoveredBaseReelMotion target, RecoveredReelView view,
            float seconds, Func<IReadOnlyList<int>> result, Action<RecoveredReelView> completed, int frame)
        {
            // Original WaitForSeconds uses TimeSpan.FromMilliseconds(seconds * 1000f).
            delay = (float)TimeSpan.FromMilliseconds(seconds * 1000f).TotalSeconds;
            if (delay < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            motion = target; reel = view; column = result; callback = completed; initialFrame = frame;
        }
        public void GetResult()
        {
            if (!IsCompleted) throw new InvalidOperationException("Stop operation is not complete.");
            if (Error != null) throw Error;
        }
        bool IRecoveredReelUpdateItem.Step(int frame, float scaledDeltaTime)
        {
            if (phase >= 2) return false;
            try {
                if (phase == 0) {
                    if (elapsed == 0 && frame == initialFrame) return true;
                    elapsed += scaledDeltaTime;
                    if (elapsed < delay) return true;
                    motion.RequestStop(column); phase = 1;
                    RecoveredReelStopLoop.Requeue(this);
                    return false; // Delay item completes; WaitUntil is a newly queued loop item.
                }
                if (motion.StopRequested) return true;
                phase = 2;
                callback?.Invoke(reel);
                phase = 3;
            } catch (Exception error) { Error = error; phase = 3; }
            Continuation?.Invoke();
            return false;
        }
    }

    // The original PlayerLoopTiming.Update runner precedes ScriptRunBehaviourUpdate.
    // Install at that same native loop boundary, independent of MonoBehaviour script ordering.
    internal static class RecoveredReelStopLoop
    {
        private sealed class StopUpdate { }
        private static readonly List<IRecoveredReelUpdateItem> pending = new List<IRecoveredReelUpdateItem>(8);
        private static readonly List<IRecoveredReelUpdateItem> waiting = new List<IRecoveredReelUpdateItem>(8);
        private static bool installed, running;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { pending.Clear(); waiting.Clear(); running = false; installed = false; }
        internal static RecoveredReelStopOperation Schedule(RecoveredBaseReelMotion motion, RecoveredReelView reel,
            float seconds, Func<IReadOnlyList<int>> column, Action<RecoveredReelView> callback)
        {
            EnsureInstalled();
            var operation = new RecoveredReelStopOperation(motion,reel,seconds,column,callback,Time.frameCount);
            Requeue(operation); return operation;
        }
        internal static void Requeue(IRecoveredReelUpdateItem operation)
        { EnsureInstalled(); if (running) waiting.Add(operation); else pending.Add(operation); }
        private static void EnsureInstalled()
        {
            if (installed) return;
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            for (int i = 0; i < loop.subSystemList.Length; i++) {
                var update = loop.subSystemList[i];
                if (update.type != typeof(UnityEngine.PlayerLoop.Update)) continue;
                var previous = update.subSystemList;
                for (int j = 0; j < previous.Length; j++) if (previous[j].type == typeof(StopUpdate)) { installed = true; return; }
                var next = new PlayerLoopSystem[previous.Length + 1];
                next[0] = new PlayerLoopSystem { type = typeof(StopUpdate), updateDelegate = Tick };
                Array.Copy(previous,0,next,1,previous.Length);
                update.subSystemList = next; loop.subSystemList[i] = update;
                PlayerLoop.SetPlayerLoop(loop); installed = true; return;
            }
            throw new InvalidOperationException("Unity Update loop was not found.");
        }
        private static void Tick()
        {
            running = true;
            // Original PlayerLoopRunner.RunCore 0x44b32ac moves a live tail item into a hole.
            // Tail candidates are advanced during that scan, preserving callback order.
            int end = pending.Count - 1;
            for (int i = 0; i <= end; i++) {
                if (pending[i].Step(Time.frameCount,Time.deltaTime)) continue;
                bool filled = false;
                while (i < end) {
                    var candidate = pending[end]; pending[end] = null; end--;
                    if (!candidate.Step(Time.frameCount,Time.deltaTime)) continue;
                    pending[i] = candidate; filled = true; break;
                }
                if (!filled) { end = i - 1; break; }
            }
            if (end + 1 < pending.Count) pending.RemoveRange(end + 1,pending.Count - end - 1);
            running = false;
            pending.AddRange(waiting); waiting.Clear();
        }
    }
}
