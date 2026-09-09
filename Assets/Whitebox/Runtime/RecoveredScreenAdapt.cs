using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Adapt 238c094..238c4d8: applies at Awake, OnEnable, Start or explicit refresh.
    public sealed class RecoveredScreenAdapt : MonoBehaviour
    {
        [SerializeField] private CanvasScaler uiRootScaler;
        private CanvasScaler scaler;
        private RectTransform rect;
        private bool applying;
        public Rect LastSafeArea { get; private set; }
        public int LastWidth { get; private set; }
        public int LastHeight { get; private set; }
        private void Awake(){rect=GetComponent<RectTransform>();CacheScaler();AdaptScreen();}
        private void OnEnable()=>AdaptScreen();
        private void Start()=>AdaptScreen();
        // The owning UI root supplies its scaler when it is not in this parent chain.
        public void BindUiRootScaler(CanvasScaler value){uiRootScaler=value;CacheScaler();}
        private void CacheScaler(){scaler=GetComponentInParent<CanvasScaler>();if(scaler==null)scaler=uiRootScaler;}
        public void AdaptScreen()=>Apply(Screen.width,Screen.height,Screen.safeArea);
        public void Apply(int width,int height,Rect safeArea)
        {
            if(applying)return;
            if(rect==null)rect=GetComponent<RectTransform>();if(scaler==null)CacheScaler();
            if(rect==null||scaler==null||width<=1||height<=1||!(safeArea.width>1)||!(safeArea.height>1))return;
            applying=true;LastSafeArea=safeArea;LastWidth=width;LastHeight=height;
            Vector2 reference=scaler.referenceResolution;float match=scaler.matchWidthOrHeight;
            float matchedHeight=match*reference.y;
            float conversion=matchedHeight/height-reference.x*(match-1)/width;
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=new Vector2(safeArea.x*conversion,safeArea.y*conversion);
            float maximumX=(safeArea.x*conversion+safeArea.width*conversion)-(reference.x*(1-match)+match*(reference.y*width/height));
            float maximumY=-((matchedHeight-(match-1)*(reference.x*height/width))-safeArea.y*conversion-safeArea.height*conversion);
            rect.offsetMax=new Vector2(maximumX,maximumY);
            applying=false;
        }
    }
}
