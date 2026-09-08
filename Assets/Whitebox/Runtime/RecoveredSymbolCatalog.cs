using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    [CreateAssetMenu(menuName = "Dragon Legend/Symbol Catalog")]
    public sealed class RecoveredSymbolCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Definition
        {
            public string originalName;
            public int id;
            public string spritePath;
            public string blurSpritePath;
            public string[] originalEffectPaths; // Provenance; effects are not yet converted or played.
            [NonSerialized] private Sprite sharp, blurred;
            public Sprite LoadSprite(bool blur)
            {
                if (blur) { if (blurred == null) blurred = Resources.Load<Sprite>(blurSpritePath); return blurred; }
                if (sharp == null) sharp = Resources.Load<Sprite>(spritePath);
                return sharp;
            }
        }
        [SerializeField] private Definition[] definitions;
        [SerializeField] private int[] baseOrder;
        [SerializeField] private int[] freeOrder;
        public int Count => definitions.Length;
        public Definition At(int index) => definitions[index];
        public int ModeCount(RecoveredSlotType mode) => (mode == RecoveredSlotType.Base ? baseOrder : freeOrder).Length;
        public int ModeId(RecoveredSlotType mode, int index) => (mode == RecoveredSlotType.Base ? baseOrder : freeOrder)[index];
        public Definition Find(int id)
        {
            for (int i = 0; i < definitions.Length; i++) if (definitions[i].id == id) return definitions[i];
            throw new ArgumentOutOfRangeException(nameof(id), id, "No original symbol definition.");
        }
    }
}
