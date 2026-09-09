using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Adapt (0x238c094..0x238c4dc). The window supplies UIManager's root scaler.
    public sealed class RecoveredSafeArea : MonoBehaviour
    {
        private CanvasScaler scaler, rootScaler;
        private RectTransform rect;
        private bool applying;
        public void BindRootScaler(CanvasScaler value) => rootScaler = value;
        private void Awake() { rect = GetComponent<RectTransform>(); CacheScaler(); AdaptScreen(); }
        private void OnEnable() => AdaptScreen();
        private void Start() => AdaptScreen();
        private void CacheScaler()
        {
            scaler = GetComponentInParent<CanvasScaler>();
            if (scaler == null) scaler = rootScaler;
        }
        public void AdaptScreen() => Apply(Screen.width, Screen.height, Screen.safeArea);
        public void Apply(int width, int height, Rect safeArea)
        {
            if (applying) return;
            if (rect == null) rect = GetComponent<RectTransform>();
            if (scaler == null) CacheScaler();
            if (rect == null || scaler == null || width <= 1 || height <= 1 || safeArea.width <= 1 || safeArea.height <= 1) return;
            applying = true;
            Vector2 reference = scaler.referenceResolution;
            float match = scaler.matchWidthOrHeight;
            // Preserve native arithmetic rather than using CanvasScaler's logarithmic scale.
            float factor = match * reference.y / height - reference.x * (match - 1) / width;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(safeArea.x * factor, safeArea.y * factor);
            rect.offsetMax = new Vector2(
                safeArea.x * factor + safeArea.width * factor - (reference.x * (1 - match) + match * (reference.y * width / height)),
                -((match * reference.y - (match - 1) * (reference.x * height / width)) - safeArea.y * factor - safeArea.height * factor));
            applying = false;
        }
    }
}
