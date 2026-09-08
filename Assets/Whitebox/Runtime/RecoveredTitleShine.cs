using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Original UIShiny RectTransform mode. Keeps packed UVs and byte parameters.
    [RequireComponent(typeof(Image))]
    public sealed class RecoveredTitleShine : BaseMeshEffect
    {
        [SerializeField] private Material template;
        [SerializeField] private float effectFactor=.5f,width=.25f,rotation=135,softness=1,brightness=1,gloss=1;
        [SerializeField] private bool play=true,loop=true;
        [SerializeField] private float duration=2,initialPlayDelay,loopDelay;
        private Material instance,previous;
        private Texture2D parameters;
        private readonly Color32[] pixels=new Color32[2];
        private float elapsed;
        public float EffectFactor {get=>effectFactor;set{effectFactor=Mathf.Clamp01(value);WriteParameters();}}
        public bool Playing {get=>play;set=>play=value;}
        protected override void OnEnable()
        {
            base.OnEnable();
            if(template==null)return;
            previous=graphic.material;
            instance=new Material(template){hideFlags=HideFlags.DontSave};
            parameters=new Texture2D(2,1,TextureFormat.RGBA32,false,true){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};
            instance.SetTexture("_ParamTex",parameters);graphic.material=instance;WriteParameters();
            elapsed=play?-initialPlayDelay:0;Canvas.willRenderCanvases+=Advance;
        }
        private static byte Byte(float value)=>(byte)(Mathf.Clamp01(value)*255);
        private void WriteParameters()
        {
            if(parameters==null)return;
            pixels[0]=new Color32(Byte(effectFactor),Byte(width),Byte(softness),Byte(brightness));
            pixels[1]=new Color32(Byte(gloss),0,0,0);parameters.SetPixels32(pixels);parameters.Apply(false,false);
        }
        private void Advance()
        {
            if(!play)return;
            elapsed+=Time.deltaTime;float sampled=elapsed;
            if(elapsed>=duration){play=loop;elapsed=loop?-loopDelay:0;}
            EffectFactor=sampled/duration;
        }
        public override void ModifyMesh(VertexHelper vertices)
        {
            if(!IsActive()||template==null)return;
            Rect rect=graphic.rectTransform.rect;
            var direction=new Vector2(Mathf.Cos(rotation*Mathf.Deg2Rad)*rect.height/rect.width,Mathf.Sin(rotation*Mathf.Deg2Rad)).normalized;
            var vertex=new UIVertex();
            for(int i=0;i<vertices.currentVertCount;i++){
                vertices.PopulateUIVertex(ref vertex,i);
                float x=(vertex.position.x-rect.xMin)/rect.width-.5f;
                float y=(vertex.position.y-rect.yMin)/rect.height-.5f;
                float shinyY=x*direction.y+y*direction.x+.5f;
                vertex.uv0=new Vector4(Pack(vertex.uv0.x,vertex.uv0.y),Pack(shinyY,.5f),0,0);
                vertices.SetUIVertex(vertex,i);
            }
        }
        private static float Pack(float x,float y)=>(int)(Mathf.Clamp01(x)*4095)+(int)(Mathf.Clamp01(y)*4095)*4096;
        protected override void OnDisable()
        {
            Canvas.willRenderCanvases-=Advance;
            if(graphic!=null&&instance!=null)graphic.material=previous;
            if(instance!=null)Destroy(instance);if(parameters!=null)Destroy(parameters);
            instance=null;parameters=null;base.OnDisable();
        }
    }
}
