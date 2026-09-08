using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Animation applies a per-instance mesh tint without cloning or changing shared materials.
    public sealed class RecoveredMeshTint : MonoBehaviour
    {
        [SerializeField] private Renderer target;
        [SerializeField] private Color color = Color.white;
        private MaterialPropertyBlock properties;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        public Color Color => color;
        private void OnEnable() => Apply();
        private void OnDidApplyAnimationProperties() => Apply();
        private void Apply()
        {
            if (target == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            target.GetPropertyBlock(properties);
            properties.SetColor(ColorId, color);
            target.SetPropertyBlock(properties);
        }
    }
}
