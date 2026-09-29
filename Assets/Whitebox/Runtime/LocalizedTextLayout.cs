using TMPro;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // The prefab holds both authored layouts. Runtime only selects the current one.
    [DisallowMultipleComponent]
    public sealed class LocalizedTextLayout : MonoBehaviour
    {
        [SerializeField] private TMP_Text target;
        [SerializeField] private bool hasEnglishLayout;
        [SerializeField] private float englishFontSize, englishMin, englishMax;
        [SerializeField] private bool englishAutoSize, englishWrapping;
        [SerializeField] private Vector2 englishSizeDelta;
        [SerializeField] private float portugueseFontSize;
        [SerializeField] private bool portugueseWrapping;
        [SerializeField] private Vector2 portugueseSizeDelta;

        public void Configure(TMP_Text text, float size, Vector2 bounds, bool wrapping)
        {
            target = text;
            if (!hasEnglishLayout)
            {
                englishFontSize = target.fontSize;
                englishMin = target.fontSizeMin; englishMax = target.fontSizeMax;
                englishAutoSize = target.enableAutoSizing; englishWrapping = target.enableWordWrapping;
                englishSizeDelta = target.rectTransform.sizeDelta;
                hasEnglishLayout = true;
            }
            portugueseFontSize = size;
            portugueseSizeDelta = bounds;
            portugueseWrapping = wrapping;
        }

        private void OnEnable()
        {
            GameLocalization.LanguageChanged += LanguageChanged;
            Refresh();
        }
        private void OnDisable()
        {
            GameLocalization.LanguageChanged -= LanguageChanged;
            RestoreEnglish();
        }
        private void LanguageChanged(int language) { Refresh(); }
        public void Refresh()
        {
            if (target == null || !hasEnglishLayout) return;
            if (GameLocalization.CurrentLanguage != 1) { RestoreEnglish(); return; }
            target.enableAutoSizing = false;
            target.enableWordWrapping = portugueseWrapping;
            target.fontSize = portugueseFontSize;
            target.rectTransform.sizeDelta = portugueseSizeDelta;
        }
        private void RestoreEnglish()
        {
            if (target == null || !hasEnglishLayout) return;
            target.enableAutoSizing = englishAutoSize;
            target.enableWordWrapping = englishWrapping;
            target.fontSizeMin = englishMin; target.fontSizeMax = englishMax;
            target.fontSize = englishFontSize;
            target.rectTransform.sizeDelta = englishSizeDelta;
        }
    }
}
