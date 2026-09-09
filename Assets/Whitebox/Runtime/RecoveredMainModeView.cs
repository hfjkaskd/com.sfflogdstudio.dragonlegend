using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Visual consumers of Main.SetInitShow; initialization/count calls remain separate.
    public sealed class RecoveredMainModeView : MonoBehaviour
    {
        [SerializeField] private GameObject baseRoll,baseResult,freeRoll;
        [SerializeField] private RecoveredFreeReels freeReels;
        [SerializeField] private RecoveredRegionAnimator fireworks;
        [SerializeField] private Canvas fireworksCanvas;
        [SerializeField] private RecoveredFreeBottom bottom;
        [SerializeField] private RecoveredDownWinText downWin;
        private RecoveredMainBackground background;
        private RecoveredPlayerProgress player;
        private RecoveredFreeSpinResult result;
        private RecoveredSymbolCatalog symbols;
        public RecoveredFreeReels FreeReels=>freeReels;
        public RecoveredRegionAnimator Fireworks=>fireworks;
        public GameObject BaseRoll=>baseRoll;
        public GameObject BaseResult=>baseResult;
        public GameObject FreeRoll=>freeRoll;
        public void Bind(RecoveredMainBackground mainBackground,RecoveredPlayerProgress progress,
            RecoveredFreeSpinResult freeResult,RecoveredSymbolCatalog catalog)
        {
            background=mainBackground;player=progress;result=freeResult;symbols=catalog;
            var main=GetComponentInParent<Canvas>();fireworksCanvas.overrideSorting=true;
            fireworksCanvas.sortingLayerID=main.sortingLayerID;fireworksCanvas.sortingOrder=main.sortingOrder-3;
            fireworksCanvas.worldCamera=main.worldCamera;
            ApplyCurrent();
        }
        public void ApplyCurrent()
        {
            bool isBase=player.GameSlotType==RecoveredSlotType.Base;
            background.Apply(player.GameSlotType);fireworks.gameObject.SetActive(!isBase);
            bottom.Apply(player.GameSlotType);baseRoll.SetActive(isBase);freeRoll.SetActive(!isBase);
            baseResult.SetActive(isBase);
            // FreeResult is an authored child of the native FreeReels container.
            if(isBase&&downWin.TemporaryTotal!=0)downWin.ShowAmountOnly(downWin.TemporaryTotal);
            else downWin.Started();
        }
        public void InitializeFreeReels()=>freeReels.Initialize(symbols,result);
        public void RefreshFreeCount()=>bottom.RefreshCount();
    }
}
