using UnityEngine;

namespace DragonLegend.Whitebox
{
    // RollReel.SetImg 0x2374a60 visual state, carried by world-space SpriteRenderers.
    public sealed class RecoveredSymbolView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer symbol;
        [SerializeField] private SpriteRenderer cover;
        [SerializeField] private Vector3 normalScale;
        [SerializeField] private Vector3 blurScale;
        public SpriteRenderer Symbol => symbol;
        public SpriteRenderer Cover => cover;
        public void Show(RecoveredSymbolCatalog catalog, int id, RecoveredSlotType mode, bool blur = false, bool hide = false)
        {
            var definition = catalog.Find(id);
            bool useBlurSprite = mode == RecoveredSlotType.Base && blur;
            symbol.sprite = definition.LoadSprite(useBlurSprite);
            symbol.transform.localScale = useBlurSprite ? blurScale : normalScale;
            symbol.gameObject.SetActive(true);
            cover.gameObject.SetActive(mode == RecoveredSlotType.Base ? hide : blur);
        }
    }
}
