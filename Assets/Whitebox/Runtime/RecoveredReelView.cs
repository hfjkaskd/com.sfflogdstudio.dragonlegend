using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // RollReel.Init 0x23747c4, RefreshSymbol 0x2375630 and SetSymbolPos 0x2374f4c.
    public sealed class RecoveredReelView : MonoBehaviour
    {
        [SerializeField] private Transform rotationNode;
        [SerializeField] private RecoveredSymbolView symbolPrefab;
        [SerializeField] private int slotCount;
        [SerializeField] private float itemHeightPixels;
        [SerializeField] private float unitsPerPixel;
        [SerializeField] private float bottomPixels;
        [SerializeField] private int wrapSlots;
        [SerializeField] private int initialFakeInterval;
        private RecoveredSymbolView[] symbols;
        private int[] ids;
        private bool[] shownEffects;
        private RecoveredSymbolCatalog catalog;
        private RecoveredSlotType mode;
        private float offsetPixels;
        private int fakeCycle, fakeInterval;
        public event Action EffectsClearRequested;
        public event Action CoinsClearRequested;
        public event Action BallsClearRequested;
        public event Action FakeCoinRequested;
        public int SlotCount => slotCount;
        public float OffsetPixels => offsetPixels;
        public int SymbolId(int index) => ids[index];
        public RecoveredSymbolView SymbolAt(int index) => symbols[index];
        public void Initialize(RecoveredSymbolCatalog source, RecoveredSlotType slotMode)
        {
            catalog = source ?? throw new ArgumentNullException(nameof(source)); mode = slotMode;
            if (symbols == null) {
                symbols = new RecoveredSymbolView[slotCount]; ids = new int[slotCount];shownEffects=new bool[slotCount];
                for (int i = 0; i < slotCount; i++) {
                    symbols[i] = Instantiate(symbolPrefab, rotationNode, false);
                    symbols[i].gameObject.layer = gameObject.layer;
                    symbols[i].Symbol.gameObject.layer = gameObject.layer;
                    symbols[i].Cover.gameObject.layer = gameObject.layer;
                    symbols[i].transform.localPosition = new Vector3(0, (bottomPixels + itemHeightPixels * i) * unitsPerPixel, 0);
                    symbols[i].Symbol.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    symbols[i].Cover.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                }
            }
            offsetPixels = 0; fakeCycle = 0; fakeInterval = initialFakeInterval;
            Array.Clear(shownEffects,0,shownEffects.Length);
            var position = rotationNode.localPosition; position.y = 0; rotationNode.localPosition = position;
            for (int i = 0; i < slotCount; i++) { ids[i] = RandomId(); symbols[i].Show(catalog, ids[i], mode); }
        }
        public void ResetPresentation()
        {
            for (int i = 0; i < slotCount; i++) symbols[i].ResetPresentation();
        }
        public bool TryHideForEffect(int row)
        {
            if(shownEffects[row])return false;
            shownEffects[row]=true;symbols[row].Symbol.gameObject.SetActive(false);return true;
        }
        public void SetOffsetPixels(float value)
        {
            offsetPixels = value;
            var position = rotationNode.localPosition;
            position.y = value * unitsPerPixel; rotationNode.localPosition = position;
        }
        // ConstantSpeedRoll 0x2377f0c..0x23780c8: replace the supplied result rows,
        // leave the other slot IDs intact, and make all seven sprites sharp.
        public void ApplyBaseColumn(IReadOnlyList<int> column)
        {
            for (int i = 0; i < slotCount; i++) {
                if (i < column.Count) ids[i] = catalog.Find(column[i]).id;
                symbols[i].Show(catalog, ids[i], RecoveredSlotType.Base);
            }
        }
        private int RandomId() => catalog.ModeId(mode, UnityEngine.Random.Range(0, catalog.ModeCount(mode)));
        // CheckFakeCoin(true) 0x2375bbc: ordinary results replace only slot zero,
        // then SetImg(..., true, false) retains the Free cover. Unknown IDs leave it alone.
        public void ApplyFreeInitialSymbol(int id)
        {
            for (int i = 0; i < catalog.ModeCount(RecoveredSlotType.Free); i++) {
                if (catalog.ModeId(RecoveredSlotType.Free,i) != id) continue;
                ids[0] = id;
                symbols[0].Show(catalog,id,RecoveredSlotType.Free,true);
                return;
            }
        }
        public void Refresh(float speedPixelsPerSecond, float deltaTime, bool blur)
        {
            float next = offsetPixels - speedPixelsPerSecond * deltaTime;
            float span = itemHeightPixels * wrapSlots;
            bool wrapped = next < -span;
            // Native performs one conditional wrap, even if still out of range afterwards.
            offsetPixels = wrapped ? next + span : next;
            var position = rotationNode.localPosition; position.y = offsetPixels * unitsPerPixel; rotationNode.localPosition = position;
            if (!wrapped) return;
            EffectsClearRequested?.Invoke(); CoinsClearRequested?.Invoke(); BallsClearRequested?.Invoke();
            Array.Clear(shownEffects,0,shownEffects.Length);
            for (int i = 0; i < slotCount; i++) {
                int id = i < slotCount - wrapSlots ? ids[i + wrapSlots] : RandomId();
                symbols[i].Show(catalog, id, mode, blur); ids[i] = id;
            }
            if (mode == RecoveredSlotType.Free && ++fakeCycle >= fakeInterval) {
                fakeCycle = 0; fakeInterval = UnityEngine.Random.Range(4, 6); FakeCoinRequested?.Invoke();
            }
        }
    }
}
