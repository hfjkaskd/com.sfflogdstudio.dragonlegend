using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // UICashOutTipView.OnBeforeShow / OnClickButton, 23abcb8 / 23abed8.
    public sealed class RecoveredCashPromptWindow : MonoBehaviour
    {
        [SerializeField] private RectTransform content, fingerPrefab;
        [SerializeField] private Text cashText;
        [SerializeField] private Button claim;
        [SerializeField] private float duration;
        [SerializeField] private AnimationCurve enterEase, exitEase;
        [SerializeField] private string showSound;
        private RectTransform finger;
        private Action completed, openWithdraw;
        private int phase;
        private float elapsed;
        public Button ClaimButton=>claim;
        public Text CashText=>cashText;
        public RectTransform Finger=>finger;
        public event Action<string> SoundRequested;
        private void Awake()=>claim.onClick.AddListener(Claim);
        public void Bind(Camera camera,Action withdraw)
        {GetComponent<Canvas>().worldCamera=camera;openWithdraw=withdraw;}
        public void Show(int tier,RecoveredGameplayRules rules,int language,Action callback)
        {
            if(gameObject.activeSelf)return;
            gameObject.SetActive(true);SoundRequested?.Invoke(showSound);completed=callback;
            cashText.text=RecoveredCurrency.Format(rules.GetCashOutCash(tier),language,0);
            if(finger==null)finger=Instantiate(fingerPrefab,claim.transform,false);
            finger.anchoredPosition=Vector2.zero;finger.localScale=Vector3.one;finger.gameObject.SetActive(true);
            content.localScale=Vector3.zero;elapsed=0;phase=1;
        }
        private void Claim()
        {
            SoundRequested?.Invoke("click");finger.gameObject.SetActive(false);
            content.localScale=Vector3.one;elapsed=0;phase=2;
            // Native releases Main's wait immediately, before opening the withdrawal view.
            completed?.Invoke();openWithdraw();
        }
        private void Update()
        {
            if(phase==0)return;elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/duration);
            content.localScale=Vector3.one*(phase==1?enterEase.Evaluate(t):1-exitEase.Evaluate(t));
            if(t<1)return;bool hiding=phase==2;phase=0;if(hiding)gameObject.SetActive(false);
        }
        public void Cancel(){completed=null;phase=0;if(finger!=null)finger.gameObject.SetActive(false);gameObject.SetActive(false);}
    }
}
