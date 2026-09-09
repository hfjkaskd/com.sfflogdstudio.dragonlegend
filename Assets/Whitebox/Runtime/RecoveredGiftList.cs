using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // InitGiftItems 23a9a3c: original catalog contains one item (id 0, Amazon).
    public sealed class RecoveredGiftList : MonoBehaviour
    {
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RecoveredGiftItem template;
        [SerializeField] private RectTransform frame;
        [SerializeField] private Vector2 cellSize;
        [SerializeField] private Vector3 hidePosition;
        [SerializeField] private float movementThresholdSquared;
        private RecoveredGameplayRules rules;private RecoveredPlayerProgress player;private int language;
        private RecoveredGiftItem item;private Vector2 viewSize;private Vector3 previousPosition;private bool initialized,dirty;
        public RecoveredGiftItem Item=>item;
        public RectTransform SelectionFrame=>frame;
        public ScrollRect Scroll=>scroll;
        public void Bind(RecoveredGameplayRules config,RecoveredPlayerProgress progress,int currencyLanguage,RectTransform selectionFrame)
        {rules=config;player=progress;language=currencyLanguage;frame=selectionFrame;}
        public void RefreshData()
        {
            if(!initialized){
                viewSize=((RectTransform)transform.parent).rect.size;((RectTransform)transform).sizeDelta=viewSize;
                scroll.content.sizeDelta=new Vector2(Mathf.Max(viewSize.x,cellSize.x),Mathf.Max(viewSize.y,cellSize.y));
                initialized=true;
            }
            dirty=true;
        }
        private void Update()
        {
            if(!initialized)return;
            Vector3 position=scroll.content.localPosition;
            if((position-previousPosition).sqrMagnitude>movementThresholdSquared)previousPosition=position;else if(!dirty)return;
            Vector2 center=viewSize*.5f,half=cellSize*.5f;
            bool visible=!(center.x+half.x<Mathf.Abs(position.x+half.x-center.x)||center.y+half.y<Mathf.Abs(position.y-half.y+center.y));
            if(!visible){if(item!=null)item.transform.localPosition=hidePosition;}
            else {
                if(item==null)item=Instantiate(template,scroll.content,false);
                item.transform.localPosition=new Vector3(half.x,-half.y,0);item.transform.localScale=Vector3.one;
                item.Refresh(rules,player,language,0,frame);
            }
            dirty=false;
        }
    }
}
