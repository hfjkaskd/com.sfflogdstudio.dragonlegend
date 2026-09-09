using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Original ListView's fixed-size vertical path used by UICollectView.
    public sealed class RecoveredCollectList : MonoBehaviour
    {
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RecoveredCollectItem template;
        [SerializeField] private Vector2 cellSize;
        [SerializeField] private int columns;
        [SerializeField] private Vector3 hidePosition;
        [SerializeField] private float movementThresholdSquared;
        private RecoveredGameplayRules rules;
        private RecoveredPlayerProgress player;
        private RecoveredCollectItem[] shown;
        private Vector3[] positions;
        private readonly List<RecoveredCollectItem> cache = new List<RecoveredCollectItem>();
        private readonly List<int> hidden = new List<int>();
        private Vector2 viewSize;
        private Vector3 previousPosition;
        private bool dataDirty;
        public ScrollRect Scroll => scroll;
        public int TotalCount => shown == null ? 0 : shown.Length;
        public int CreatedCount { get; private set; }
        public int CachedCount => cache.Count;
        public RecoveredCollectItem ItemAt(int index) => shown[index];
        public void Initialize(RecoveredGameplayRules config, RecoveredPlayerProgress progress, Vector2 size)
        {
            rules = config; player = progress; viewSize = size;
            ((RectTransform)transform).sizeDelta = size;
            int total = config.GetCollectInfos().Count;
            shown = new RecoveredCollectItem[total]; positions = new Vector3[total]; hidden.Capacity = total;
            for (int i = 0; i < total; i++) positions[i] = new Vector3((i % columns + .5f) * cellSize.x, -(i / columns + .5f) * cellSize.y, 0);
            int rows = Mathf.Max(1, (total + columns - 1) / columns);
            scroll.content.sizeDelta = new Vector2(Mathf.Max(size.x, columns * cellSize.x), Mathf.Max(size.y, rows * cellSize.y));
            template.transform.localPosition = hidePosition;
            dataDirty = true;
        }
        public void RefreshData() => dataDirty = true;
        private void Update()
        {
            if (shown == null) return;
            Vector3 position = scroll.content.localPosition;
            if ((position - previousPosition).sqrMagnitude > movementThresholdSquared) previousPosition = position;
            else if (!dataDirty) return;
            // Check existing cells first, returning offscreen cells before acquiring replacements.
            hidden.Clear();
            for (int i = 0; i < shown.Length; i++) {
                if (shown[i] == null) hidden.Add(i);
                else if (!Visible(i, position)) {
                    shown[i].transform.localPosition = hidePosition;
                    cache.Add(shown[i]); shown[i] = null;
                }
                else if (dataDirty) shown[i].Initialize(rules.GetCollectInfos()[i], player);
            }
            for (int j = 0; j < hidden.Count; j++) {
                int i = hidden[j]; if (!Visible(i, position)) continue;
                RecoveredCollectItem item;
                if (cache.Count == 0) {
                    item = Instantiate(template, scroll.content, false); item.transform.localScale = Vector3.one; CreatedCount++;
                }
                else { item = cache[0]; cache.RemoveAt(0); }
                shown[i] = item;
                if (i == 0 || shown[i - 1] == null) item.transform.SetAsFirstSibling(); else item.transform.SetAsLastSibling();
                item.transform.localPosition = positions[i];
                item.Initialize(rules.GetCollectInfos()[i], player);
            }
            dataDirty = false;
        }
        private bool Visible(int i, Vector3 position)
        {
            Vector2 center = viewSize * .5f, half = cellSize * .5f;
            return !(center.x + half.x < Mathf.Abs(position.x + positions[i].x - center.x) ||
                center.y + half.y < Mathf.Abs(position.y + positions[i].y + center.y));
        }
    }
}
