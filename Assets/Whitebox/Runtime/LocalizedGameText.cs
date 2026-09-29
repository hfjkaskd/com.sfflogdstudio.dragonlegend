using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    [DisallowMultipleComponent]
    public sealed class LocalizedGameText : MonoBehaviour
    {
        [SerializeField] private TMP_Text tmpText;
        [SerializeField] private Text legacyText;
        [SerializeField] private string key;
        [SerializeField, TextArea] private string englishFallback;
        public string Key => key;
        public string EnglishFallback => englishFallback;

        public void Configure(TMP_Text tmp, Text legacy, string textKey, string english)
        {
            tmpText = tmp;
            legacyText = legacy;
            key = textKey;
            englishFallback = english;
        }

        private void OnEnable()
        {
            GameLocalization.LanguageChanged += OnLanguageChanged;
            Refresh();
        }

        private void OnDisable() => GameLocalization.LanguageChanged -= OnLanguageChanged;
        private void OnLanguageChanged(int language) => Refresh();

        public void Refresh()
        {
            string value = GameLocalization.Get(key, englishFallback);
            if (tmpText != null && tmpText.text != value) tmpText.text = value;
            if (legacyText != null && legacyText.text != value) legacyText.text = value;
        }
    }
}
