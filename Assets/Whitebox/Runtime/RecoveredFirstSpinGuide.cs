using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Guide.Init 2371d20 / SetGuideInfo 23721f0 / ResetGuideCollect 237251c.
    public sealed class RecoveredFirstSpinGuide : MonoBehaviour
    {
        [SerializeField] private RectTransform node;
        [SerializeField] private RecoveredGuideText text;
        [SerializeField] private Button background;
        private RectTransform target;
        private Transform originalParent;
        public RecoveredGuideText Text=>text;
        public Button Background=>background;
        public RectTransform Node=>node;
        public RectTransform Target=>target;
        // Source Bg has no persistent calls; Guide.Init/constructor bind no click listener.
        public void Show(RectTransform main,RectTransform spin,Camera camera,int step=1)
        {
            Hide();gameObject.SetActive(true);background.gameObject.SetActive(true);
            Vector2 point;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(main,
                RectTransformUtility.WorldToScreenPoint(camera,spin.position),camera,out point);
            float y=point.y+node.sizeDelta.y*.5f+spin.sizeDelta.y*.5f;
            if(y>Screen.height/2)y=point.y-node.sizeDelta.y*.5f-spin.sizeDelta.y*.5f;
            node.anchoredPosition=new Vector2(0,y);
            target=spin;originalParent=spin.parent;
            spin.SetParent(transform,true);
            text.SetText(step);
        }
        public void Hide()
        {
            if(target!=null&&originalParent!=null)target.SetParent(originalParent,true);
            target=null;originalParent=null;
            if(text!=null)text.Cancel();gameObject.SetActive(false);
        }
        private void OnDestroy()
        {
            if(target!=null&&originalParent!=null)target.SetParent(originalParent,true);
            if(text!=null)text.Cancel();
        }
    }
}
