using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredBonusCollection : MonoBehaviour
    {
        [Serializable] public sealed class Column { public RectTransform[] items; }
        [SerializeField] private Column[] columns;
        // BonusCoins.Init 0x23b8154 and InitSelect 0x23b82a4.
        public void Initialize(IReadOnlyList<int> counts)
        {
            for (int column = 0; column < columns.Length; column++)
                for (int item = 0; item < columns[column].items.Length; item++)
                    columns[column].items[item].GetChild(0).gameObject.SetActive(counts[column] >= item + 1);
        }
        // Original GetUnSelect chooses by 1-based count, not activeSelf.
        public RectTransform GetUnselectedTarget(int column, int count)
        {
            if (count < 1 || count >= 3) return null;
            var items = columns[column].items;
            return count <= items.Length ? items[count - 1] : null;
        }
    }
}
