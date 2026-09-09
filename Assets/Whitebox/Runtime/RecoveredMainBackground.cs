using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // The BaseBg/FreeBg pair switched by Main.SetInitShow 23bca34.
    public sealed class RecoveredMainBackground : MonoBehaviour
    {
        [SerializeField] private Image baseBackground, freeBackground;
        [SerializeField] private Canvas backgroundCanvas;
        public Canvas BackgroundCanvas => backgroundCanvas;
        public void Bind(Canvas main)
        {
            // Enable the sorting override after the standalone prefab becomes nested.
            backgroundCanvas.overrideSorting = true;
            backgroundCanvas.worldCamera = main.worldCamera;
            backgroundCanvas.sortingLayerID = main.sortingLayerID;
            backgroundCanvas.sortingOrder = main.sortingOrder - 4;
        }
        public Image BaseBackground => baseBackground;
        public Image FreeBackground => freeBackground;
        public void Apply(RecoveredSlotType mode)
        {
            bool isBase = mode == RecoveredSlotType.Base;
            baseBackground.gameObject.SetActive(isBase);
            freeBackground.gameObject.SetActive(!isBase);
        }
    }
}
