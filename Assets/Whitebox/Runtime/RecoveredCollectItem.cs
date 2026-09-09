using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // CollectItem.InitUI 23ac03c. This is collection display, not a claim button.
    public sealed class RecoveredCollectItem : MonoBehaviour
    {
        [Serializable] private sealed class SpritePath { public int id; public string path; }
        [SerializeField] private Image icon;
        [SerializeField] private GameObject redPoint;
        [SerializeField] private TMP_Text pointText;
        [SerializeField] private SpritePath[] icons;
        [SerializeField] private Color missingColor, collectedColor;
        [SerializeField] private string countFormat;
        public Image Icon => icon;
        public GameObject RedPoint => redPoint;
        public TMP_Text PointText => pointText;

        public void Initialize(RecoveredCollectInfo info, RecoveredPlayerProgress player)
        {
            Sprite sprite = null;
            for (int i = 0; i < icons.Length; i++) if (icons[i].id == info.id) {
                sprite = Resources.Load<Sprite>(icons[i].path); break;
            }
            icon.sprite = sprite; icon.SetNativeSize();
            var record = player.GetPlayerCollectData(info.id);
            if (record == null) {
                icon.color = missingColor; redPoint.SetActive(false);
                // The hidden count text retains its prior value when this item is reused.
                return;
            }
            icon.color = collectedColor; redPoint.SetActive(true);
            pointText.text = string.Format(countFormat, record.count);
        }
    }
}
