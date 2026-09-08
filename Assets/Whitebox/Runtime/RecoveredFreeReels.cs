using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // UIMainView.InitFreeReels 0x23bd420 and FreeRoll.Init 0x23b8440.
    public sealed class RecoveredFreeReels : MonoBehaviour
    {
        [SerializeField] private RecoveredReelView[] reels;
        public bool IsInitialized { get; private set; }
        public RecoveredReelView At(int column, int row) => reels[column * 3 + row];

        public void Initialize(RecoveredSymbolCatalog catalog, Action<int,int,RecoveredReelView> initialEffects)
        {
            if (IsInitialized) return;
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (initialEffects == null) throw new ArgumentNullException(nameof(initialEffects));
            IsInitialized = true;
            for (int column = 0; column < 5; column++)
                for (int row = 0; row < 3; row++) {
                    var reel = At(column,row);
                    reel.Initialize(catalog,RecoveredSlotType.Free);
                    // CheckFakeCoin(true) must occur before the next reel consumes its random IDs.
                    initialEffects(column,row,reel);
                }
        }
    }
}
