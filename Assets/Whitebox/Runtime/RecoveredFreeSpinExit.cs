using System;
using System.Collections.Generic;

namespace DragonLegend.Whitebox
{
    // CheckFreeSpinEnd 0x23ca10c and transition callbacks 0x23bf600/0x23bf618.
    // The prefab presenter acknowledges the real popup and animation callbacks.
    public sealed class RecoveredFreeSpinExit
    {
        private readonly RecoveredPlayerProgress progress;
        private readonly RecoveredFreeSpinEntry entry;
        private int stage;
        public bool IsWaiting => stage != 0;
        public event Action PauseMusicRequested;
        public event Action<int> EndViewRequested;
        public event Action TransitionSoundRequested; // original sound: transform
        public event Action TransitionRequested;
        public event Action BaseViewResetRequested;
        public event Action BaseReelsInitRequested;
        public event Action FreeEndFlagClearRequested;
        public event Action BaseMusicRequested; // original BGM: normalBg

        public RecoveredFreeSpinExit(RecoveredPlayerProgress progress, RecoveredFreeSpinEntry entry)
        {
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }

        // initialSpinCount is original UIMainView.tFreeSpinCount, not remaining count.
        public void Check(int initialSpinCount, IReadOnlyList<int> symbolIds)
        {
            if (IsWaiting) return;
            if (progress.FreeSpinCount != 0) { entry.TryBegin(symbolIds); return; }
            stage = 1;
            progress.GameSlotType = RecoveredSlotType.Base;
            PauseMusicRequested?.Invoke();
            EndViewRequested?.Invoke(initialSpinCount);
        }

        public void CompleteEndView()
        {
            if (stage != 1) return;
            stage = 2;
            TransitionSoundRequested?.Invoke();
            TransitionRequested?.Invoke();
        }

        public void OnTransitionEvent()
        {
            if (stage != 2) return;
            BaseViewResetRequested?.Invoke();
            BaseReelsInitRequested?.Invoke();
        }

        public void OnTransitionComplete()
        {
            if (stage != 2) return;
            stage = 0;
            FreeEndFlagClearRequested?.Invoke();
            BaseMusicRequested?.Invoke();
        }
    }
}