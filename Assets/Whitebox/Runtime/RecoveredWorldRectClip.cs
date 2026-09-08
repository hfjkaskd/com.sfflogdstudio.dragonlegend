using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // The original mini reel RectMask2D clips both skeletons and reward labels.
    // World meshes use the same rectangle in its own coordinate frame; labels use
    // Unity's native CanvasRenderer rectangle clipping in their root canvas frame.
    public sealed class RecoveredWorldRectClip : MonoBehaviour
    {
        [SerializeField] private MeshRenderer[] meshes;
        [SerializeField] private Text[] labels;
        [SerializeField] private Transform[] canvasFrames;
        private MaterialPropertyBlock properties;
        private SpriteMask boundary;
        private static readonly int Enabled=Shader.PropertyToID("_ReelClipEnabled");
        private static readonly int Matrix=Shader.PropertyToID("_WorldToReelClip");
        private static readonly int Rectangle=Shader.PropertyToID("_ReelClipRect");
        public SpriteMask Boundary=>boundary;

        public void Bind(SpriteMask value) {boundary=value;Apply();}
        private void OnEnable(){Canvas.willRenderCanvases+=Apply;}
        private void OnDisable(){Canvas.willRenderCanvases-=Apply;}
        private void LateUpdate()=>Apply();
        private void Apply()
        {
            if(properties==null)properties=new MaterialPropertyBlock();
            bool active=boundary!=null&&boundary.sprite!=null;
            var bounds=active?boundary.sprite.bounds:default;
            var matrix=active?boundary.transform.worldToLocalMatrix:Matrix4x4.identity;
            var rectangle=new Vector4(bounds.min.x,bounds.min.y,bounds.max.x,bounds.max.y);
            for(int i=0;i<meshes.Length;i++) {
                meshes[i].GetPropertyBlock(properties);
                properties.SetFloat(Enabled,active?1:0);
                properties.SetMatrix(Matrix,matrix);properties.SetVector(Rectangle,rectangle);
                meshes[i].SetPropertyBlock(properties);
            }
            for(int i=0;i<labels.Length;i++) {
                var renderer=labels[i].canvasRenderer;
                if(!active){renderer.DisableRectClipping();continue;}
                var toCanvas=canvasFrames[i].worldToLocalMatrix*boundary.transform.localToWorldMatrix;
                var a=toCanvas.MultiplyPoint3x4(bounds.min);
                var b=toCanvas.MultiplyPoint3x4(bounds.max);
                renderer.EnableRectClipping(Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y)));
                renderer.clippingSoftness=Vector2.zero;
            }
        }
    }
}
