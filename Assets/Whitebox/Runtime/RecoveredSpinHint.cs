using UnityEngine;

namespace DragonLegend.Whitebox
{
    // InitSpinSequence 23ba59c; callback 23bf46c; accepted Spin 23bd970..23bd9b4.
    public sealed class RecoveredSpinHint : MonoBehaviour
    {
        [SerializeField] private RectTransform fingerPrefab;
        [SerializeField] private Transform destination;
        [SerializeField] private float delay;
        private RectTransform finger;
        private RecoveredReelWait wait;
        public RectTransform Finger=>finger;
        public bool IsWaiting=>wait!=null;
        public void Begin()
        {
            wait?.Cancel();
            wait=RecoveredReelWait.Delay(delay,Show,error=>{wait=null;Debug.LogException(error,this);});
        }
        private void Show()
        {
            wait=null;
            if(finger==null)finger=Instantiate(fingerPrefab,destination,false);
            else finger.SetParent(destination,false);
            finger.localScale=Vector3.one;finger.anchoredPosition=Vector2.zero;
            finger.gameObject.SetActive(true);
        }
        public void Hide()
        {
            if(finger!=null)finger.gameObject.SetActive(false);
            wait?.Cancel();wait=null;
        }
        private void OnDestroy()=>Hide();
    }
}
