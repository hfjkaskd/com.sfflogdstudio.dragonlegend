using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // ClickSpin 0x23d1f30 through its all-reels-stopped wait; reward sequence follows separately.
    public sealed class RecoveredBaseReelController : MonoBehaviour
    {
        [SerializeField] private RecoveredReelView[] reels;
        [SerializeField] private RecoveredBaseReelMotion[] motions;
        [SerializeField] private float accelerationSeconds;
        [SerializeField] private float startInterval;
        [SerializeField] private float anticipationSpeed;
        [SerializeField] private float anticipationDelay;
        [SerializeField] private float anticipationStopDelay;
        private Func<int, IReadOnlyList<int>> resultColumn;
        private int nextReel, selectedIndex;
        public int ReelCount => reels.Length;
        public int StoppedCount { get; private set; }
        public bool IsRunning { get; private set; }
        public Exception Error { get; private set; }
        public RecoveredReelView ReelAt(int index) => reels[index];
        public RecoveredBaseReelMotion MotionAt(int index) => motions[index];
        public event Action GoodLuckRequested;
        public event Action<int, bool> AnticipationVisibilityRequested;
        public event Action<int> StopAnimationRequested;
        public event Action ShakeRequested;
        public event Action ReelStopSoundRequested;
        public event Action SpeedupSoundRequested;
        public event Action SpeedupSoundStopRequested;
        public event Action<int> VibrationRequested;
        public event Action ReelsStopped;

        public void Initialize(RecoveredSymbolCatalog catalog)
        {
            for (int i = 0; i < reels.Length; i++) reels[i].Initialize(catalog, RecoveredSlotType.Base);
        }
        public void Begin(int accelerationStartIndex, Func<int, IReadOnlyList<int>> currentColumn)
        {
            if (IsRunning) throw new InvalidOperationException("Reel sequence is already running.");
            resultColumn = currentColumn ?? throw new ArgumentNullException(nameof(currentColumn));
            selectedIndex = accelerationStartIndex; nextReel = 0; StoppedCount = 0; Error = null; IsRunning = true;
            try {
                GoodLuckRequested?.Invoke();
                for (int i = 0; i < reels.Length; i++) AnticipationVisibilityRequested?.Invoke(i, false);
                StartNext();
            } catch (Exception error) { Fail(error); }
        }
        private void StartNext()
        {
            if (nextReel == reels.Length) {
                RecoveredReelWait.Until(() => StoppedCount == reels.Length, Complete, Fail);
                return;
            }
            int index = nextReel++;
            bool automatic = selectedIndex == -1 || index < selectedIndex;
            var operation = motions[index].StartBaseSpin(accelerationSeconds, index, () => resultColumn(index),
                automatic ? null : (Action<int>)BeginAnticipation, automatic ? (Action<int>)NormalStopped : null, selectedIndex);
            if (operation.IsCompleted && operation.Error != null) Fail(operation.Error);
            // Native awaits the interval even after starting the final column.
            RecoveredReelWait.Delay(startInterval, StartNext, Fail);
        }
        private void NormalStopped(int index)
        {
            StopAnimationRequested?.Invoke(index);
            ShakeRequested?.Invoke();
            StoppedCount++;
            ReelStopSoundRequested?.Invoke();
        }
        private void BeginAnticipation(int index)
        {
            // Native async-void callback starts synchronously, then waits twice for 0.5 seconds.
            SpeedupSoundRequested?.Invoke();
            AnticipationVisibilityRequested?.Invoke(index, true);
            motions[index].SetMaxSpeed(anticipationSpeed);
            RecoveredReelWait.Delay(anticipationDelay, () => {
                var stop = motions[index].SetStop(anticipationStopDelay, () => resultColumn(index));
                stop.Continuation = () => {
                    try {
                        stop.GetResult();
                        RecoveredReelWait.Until(() => !motions[index].StopRequested,
                            () => AnticipationStopped(index), Fail);
                    } catch (Exception error) { Fail(error); }
                };
            }, Fail);
        }
        private void AnticipationStopped(int index)
        {
            // b__2 receives index+1; it presents the previous column and invokes b__1 for the next.
            SpeedupSoundStopRequested?.Invoke();
            AnticipationVisibilityRequested?.Invoke(index, false);
            StopAnimationRequested?.Invoke(index);
            ShakeRequested?.Invoke();
            VibrationRequested?.Invoke(200);
            if (index + 1 < reels.Length) BeginAnticipation(index + 1);
            else StoppedCount = reels.Length;
        }
        private void Complete() { IsRunning = false; ReelsStopped?.Invoke(); }
        private void Fail(Exception error) { Error = error; }
    }
}
