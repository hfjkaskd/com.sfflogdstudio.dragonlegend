using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCashOutEntry : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform fingerTarget;
        [SerializeField] private RectTransform fingerPrefab;
        private RectTransform finger;
        private RecoveredRegionAnimator fingerAnimator;
        private RecoveredPlayerProgress player;
        private RecoveredGameplayRules rules;
        private Action open;
        public Button Button=>button;
        public RectTransform FingerTarget=>fingerTarget;
        public RectTransform Finger=>finger;
        public void Bind(Action show,RecoveredPlayerProgress progress,RecoveredGameplayRules config)
        {
            Unbind();open=show;player=progress;rules=config;button.onClick.AddListener(Click);
            player.GreenCountChanged+=GreenChanged;RefreshFinger();
        }
        private void GreenChanged(float before,float after)=>RefreshFinger();
        private void RefreshFinger()
        {
            // Main.ShowCashOutFinger 23b9ee4: first tier without any record of that ID.
            for(int tier=0;tier<rules.GetCashOutCount();tier++){
                bool found=false;
                foreach(var record in player.CashOutRecords)if(record.id==tier){found=true;break;}
                if(found)continue;
                // The event fires before balance mutation. Below threshold leaves the old finger unchanged.
                if(player.GreenCount<rules.GetCashOutCash(tier))return;
                if(finger==null){finger=Instantiate(fingerPrefab,fingerTarget,false);fingerAnimator=finger.GetComponentInChildren<RecoveredRegionAnimator>();}
                else finger.SetParent(fingerTarget,false);
                finger.localScale=Vector3.one;finger.anchoredPosition=Vector2.zero;finger.gameObject.SetActive(true);fingerAnimator.Play(0);return;
            }
            if(finger!=null)finger.gameObject.SetActive(false);
        }
        private void Click()=>open?.Invoke();
        public void Unbind(){button.onClick.RemoveListener(Click);open=null;if(player!=null)player.GreenCountChanged-=GreenChanged;player=null;rules=null;if(finger!=null)finger.gameObject.SetActive(false);}
        private void OnDestroy()=>Unbind();
    }
}
