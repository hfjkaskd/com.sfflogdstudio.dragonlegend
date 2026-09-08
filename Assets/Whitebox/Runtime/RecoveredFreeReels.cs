using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // UIMainView.InitFreeReels 0x23bd420 and FreeRoll.Init 0x23b8440.
    public sealed class RecoveredFreeReels : MonoBehaviour
    {
        [SerializeField] private RecoveredReelView[] reels;
        [SerializeField] private RecoveredFreeSpecials specials;
        [SerializeField] private RecoveredFreeReelMotion[] motions;
        public RecoveredFreeReelMotion MotionAt(int column,int row)=>motions[column*3+row];
        [SerializeField] private RecoveredFreeColumn[] columns;
        public RecoveredFreeColumn ColumnAt(int column)=>columns[column];
        [SerializeField] private RecoveredFreeReelController controller;
        public RecoveredFreeReelController Controller=>controller;
        public RecoveredFreeSpecials Specials=>specials;
        public bool IsInitialized { get; private set; }
        public RecoveredReelView At(int column, int row) => reels[column * 3 + row];

        public void Initialize(RecoveredSymbolCatalog catalog, RecoveredFreeSpinResult result,
            Action<int,int,RecoveredReelView,int> initialEffects=null)
        {
            if (IsInitialized) return;
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.IsGenerating) throw new InvalidOperationException("The initial Free result is still being generated.");
            specials.Bind(this,result);
            if (initialEffects == null) initialEffects=specials.ShowInitial;
            IsInitialized = true;
            for (int column = 0; column < 5; column++)
                for (int row = 0; row < 3; row++) {
                    var reel = At(column,row);
                    reel.Initialize(catalog,RecoveredSlotType.Free);
                    MotionAt(column,row).Bind(specials,column,row);
                    // CheckFakeCoin(true) must occur before the next reel consumes its random IDs.
                    int id=result.GetSymbol(column,row);
                    if(id==9 || id==11) initialEffects(column,row,reel,id);
                    else reel.ApplyFreeInitialSymbol(id);
                }
        }
    }
}
