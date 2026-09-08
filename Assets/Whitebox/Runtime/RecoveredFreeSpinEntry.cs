using System;
using System.Collections.Generic;

namespace DragonLegend.Whitebox
{
    // UIMainView.FreeAutoSpin 0x23bf158: debit runtime free count, then generate.
    // Its completion callback 0x23bf7d0 starts AutoFreeSpin presentation.
    public sealed class RecoveredFreeSpinEntry
    {
        private readonly RecoveredPlayerProgress progress;
        private readonly RecoveredFreeSpinResult result;
        private readonly Action onReady;
        public event Action PresentationRequested;

        public RecoveredFreeSpinEntry(RecoveredPlayerProgress progress, RecoveredFreeSpinResult result)
        {
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.result = result ?? throw new ArgumentNullException(nameof(result));
            onReady = OnReady;
        }

        public bool TryBegin(IReadOnlyList<int> symbolIds)
        {
            if (progress.FreeSpinCount < 1) return false;
            // Native generation is synchronous. Incremental generation must remain serial.
            if (result.IsGenerating) return false;
            progress.FreeSpinCount--;
            // Native does not save or refund the debit if later generation throws.
            result.Begin(symbolIds,onReady);
            return true;
        }

        private void OnReady() => PresentationRequested?.Invoke();
    }
}