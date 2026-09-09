using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Source QiPan order: LongSpine, Bg, reels/results, PlayFire.
    public sealed class RecoveredMainComposition : MonoBehaviour
    {
        [SerializeField] private Canvas dragonCanvas, boardCanvas;
        [SerializeField] private Image boardBackground;
        public Image BoardBackground => boardBackground;
        public Canvas DragonCanvas => dragonCanvas;
        public Canvas BoardCanvas => boardCanvas;
        public void Bind(Canvas main)
        {
            Configure(dragonCanvas, main, -2);
            Configure(boardCanvas, main, -1);
        }
        private static void Configure(Canvas canvas, Canvas main, int offset)
        {
            canvas.overrideSorting = true;
            canvas.sortingLayerID = main.sortingLayerID;
            canvas.sortingOrder = main.sortingOrder + offset;
            canvas.worldCamera = main.worldCamera;
        }
    }
}
